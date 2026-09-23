using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Castle.MicroKernel.Registration;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Legacy <c>PayrollService.RunPayrollEngine</c>: with <c>ManagePayrollSettings.EnablePayrollEngineAutorun</c>
/// on, one Regular run per payroll control record, up to the record's calendar month end; off, nothing.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollEngineAutorunBackgroundWorker_Tests : AppTestBase
{
    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
    private readonly List<PayrollEngineJobArgs> _enqueued = new();
    private readonly PayrollEngineAutorunBackgroundWorker _worker;

    public PayrollEngineAutorunBackgroundWorker_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _backgroundJobManager
            .EnqueueAsync<PayrollEngineBackgroundJob, PayrollEngineJobArgs>(
                Arg.Do<PayrollEngineJobArgs>(args => _enqueued.Add(args)),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>())
            .Returns(Task.FromResult("job-1"));

        LocalIocManager.IocContainer.Register(
            Component.For<IBackgroundJobManager>().Instance(_backgroundJobManager).IsDefault());

        _worker = Resolve<PayrollEngineAutorunBackgroundWorker>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void SetAutorun(bool enabled)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings { TenantId = TenantId }).Entity;
            settings.EnablePayrollEngineAutorun = enabled;
        });
    }

    private void CreateControlRecord(string areaName, DateTime monthEnd)
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
                Name = areaName,
                CurrencyId = currency.Id,
                PayrollPeriodId = period.Id,
                CutOffDay = 25
            }).Entity;
            context.SaveChanges();

            context.Set<PayrollControlRecord>().Add(new PayrollControlRecord
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = monthEnd.Month,
                PayrollYear = monthEnd.Year,
                CalendarMonthStart = new DateTime(monthEnd.Year, monthEnd.Month, 1),
                CalendarMonthEnd = monthEnd
            });
        });
    }

    [Fact]
    public void With_Autorun_On_Should_Queue_A_Regular_Run_Per_Control_Record()
    {
        SetAutorun(true);
        CreateControlRecord("PE-Auto-A", new DateTime(2024, 6, 30));
        CreateControlRecord("PE-Auto-B", new DateTime(2024, 6, 30));

        var queued = _worker.QueueRunsForTenant(TenantId);

        queued.ShouldBe(2);
        _enqueued.Count.ShouldBe(2);
        _enqueued.ShouldAllBe(a => a.PayrollRunType == PayrollRunType.Regular
                                   && a.PayInDate == new DateTime(2024, 6, 30)
                                   && a.TenantId == TenantId
                                   && a.RegisterTarget == PayrollRegisterTarget.Live);
        _enqueued.Select(a => a.PayrollAreaName).OrderBy(n => n).ShouldBe(new[] { "PE-Auto-A", "PE-Auto-B" });
    }

    [Fact]
    public void With_Autorun_Off_Should_Queue_Nothing()
    {
        SetAutorun(false);
        CreateControlRecord("PE-Auto-C", new DateTime(2024, 6, 30));

        _worker.QueueRunsForTenant(TenantId).ShouldBe(0);
        _enqueued.ShouldBeEmpty();
    }
}
