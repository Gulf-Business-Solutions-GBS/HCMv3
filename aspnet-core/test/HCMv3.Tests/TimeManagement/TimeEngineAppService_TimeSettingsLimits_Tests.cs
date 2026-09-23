using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.TimeManagement.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Legacy <c>uscTimeEngine.ascx.cs</c> lines 275-335: the two checks the Time Engine screen made before
/// queueing a run, now made by <c>TimeEngineAppService.RunTimeEngine</c>. The end date may not run more
/// than <c>ManageTimeSettings.FutureEvaluationInDays</c> past the payroll control record's month end, and
/// the start date may not precede <c>ManageTimeSettings.EarliestEvaluationDate</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_TimeSettingsLimits_Tests : AppTestBase
{
    private readonly ITimeEngineAppService _timeEngineAppService;
    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
    private readonly ITimeEngineService _timeEngineService = Substitute.For<ITimeEngineService>();

    public TimeEngineAppService_TimeSettingsLimits_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _backgroundJobManager
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
                Arg.Any<TimeEngineJobArgs>(), Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult("job-1"));

        LocalIocManager.IocContainer.Register(
            Component.For<IBackgroundJobManager>().Instance(_backgroundJobManager).IsDefault());
        LocalIocManager.IocContainer.Register(
            Component.For<ITimeEngineService>().Instance(_timeEngineService).IsDefault());

        _timeEngineAppService = Resolve<ITimeEngineAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void SetTimeSettings(int futureEvaluationInDays, DateTime earliestEvaluationDate)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                settings = context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            }

            settings.FutureEvaluationInDays = futureEvaluationInDays;
            settings.EarliestEvaluationDate = earliestEvaluationDate;
        });
    }

    /// <summary>A payroll area whose latest control record ends on <paramref name="calendarMonthEnd"/>.</summary>
    private string CreatePayrollAreaWithControlRecord(string name, DateTime calendarMonthEnd)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Dinar",
                NameAr = "Dinar",
                DecimalCount = 3,
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "KWD",
                CurrencyDenominationFormEn = "Fils",
                CurrencyDenominationFormAr = "Fils"
            }).Entity;
            var period = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Monthly",
                NameAr = "Monthly"
            }).Entity;
            context.SaveChanges();

            var area = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = name,
                CurrencyId = currency.Id,
                PayrollPeriodId = period.Id,
                CutOffDay = 25
            }).Entity;
            context.SaveChanges();

            context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = calendarMonthEnd.Month,
                PayrollYear = calendarMonthEnd.Year,
                CalendarMonthStart = new DateTime(calendarMonthEnd.Year, calendarMonthEnd.Month, 1),
                CalendarMonthEnd = calendarMonthEnd
            });
        });

        return name;
    }

    [Fact]
    public async Task Should_Refuse_An_End_Date_Beyond_The_Future_Evaluation_Limit()
    {
        SetTimeSettings(futureEvaluationInDays: 30, earliestEvaluationDate: new DateTime(2020, 1, 1));
        var area = CreatePayrollAreaWithControlRecord("TE-Limit-1", new DateTime(2024, 6, 30));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => _timeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                PayrollAreaName = area,
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 7, 31)
            }));

        exception.Message.ShouldContain("30/07/2024");
        await _backgroundJobManager.DidNotReceiveWithAnyArgs()
            .EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(null);
    }

    [Fact]
    public async Task Should_Queue_A_Run_That_Ends_Exactly_On_The_Future_Evaluation_Limit()
    {
        SetTimeSettings(futureEvaluationInDays: 30, earliestEvaluationDate: new DateTime(2020, 1, 1));
        var area = CreatePayrollAreaWithControlRecord("TE-Limit-2", new DateTime(2024, 6, 30));

        var output = await _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = area,
            StartDate = new DateTime(2024, 7, 1),
            EndDate = new DateTime(2024, 7, 30)
        });

        output.Queued.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Start_Date_Before_The_Earliest_Evaluation_Date()
    {
        SetTimeSettings(futureEvaluationInDays: 300, earliestEvaluationDate: new DateTime(2021, 4, 1));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => _timeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                EmployeeCodes = "E-NONE",
                StartDate = new DateTime(2021, 3, 31),
                EndDate = new DateTime(2021, 4, 30)
            }));

        exception.Message.ShouldContain("01/04/2021");
    }

    /// <summary>Legacy checked the end date only when it could find a control record; an unknown area has none.</summary>
    [Fact]
    public async Task Should_Not_Apply_The_Future_Limit_When_No_Control_Record_Exists()
    {
        SetTimeSettings(futureEvaluationInDays: 1, earliestEvaluationDate: new DateTime(2020, 1, 1));

        var output = await _timeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = "No such area",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2030, 12, 31)
        });

        output.Queued.ShouldBeTrue();
    }
}
