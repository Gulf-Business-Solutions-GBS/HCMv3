using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.UI;
using HCMv3.Integrations.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="PostJvAppService"/> and <see cref="CostingFileAppService"/>: the server-side refusals before
/// anything is queued, the background queuing itself, the period auto-fill and the employee popup.
/// </summary>
// ReSharper disable once InconsistentNaming
public class FinanceScreensAppService_Tests : FinancePostingTestBase
{
    private readonly IJournalVoucherBuilder _builder = Substitute.For<IJournalVoucherBuilder>();
    private readonly IBackgroundJobManager _jobs = Substitute.For<IBackgroundJobManager>();
    private readonly ID365FinancePostingSwitch _switch = Substitute.For<ID365FinancePostingSwitch>();
    private FinanceRunLog _runLog;

    public FinanceScreensAppService_Tests()
    {
        _switch.IsPostingAllowedAsync().Returns(true);
    }

    private ILocalizationManager Localization => LocalIocManager.Resolve<ILocalizationManager>();

    private CostingScreenService CreateScreen(params int[] areaEmployeeIds)
    {
        _runLog = CreateRunLog();
        return new CostingScreenService(
            Repo<PayrollArea>(),
            Repo<PayrollControlRecord>(),
            Repo<Employee>(),
            Repo<CostRegister>(),
            ResolverFor(AreaName, areaEmployeeIds),
            _builder,
            _runLog,
            AbpSession)
        {
            LocalizationManager = Localization
        };
    }

    private T Wire<T>(T service) where T : HCMv3AppServiceBase
    {
        service.AbpSession = AbpSession;
        service.LocalizationManager = Localization;
        service.UnitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        return service;
    }

    private PostJvAppService CreatePostJv(params int[] areaEmployeeIds)
    {
        var screen = CreateScreen(areaEmployeeIds);
        var preconditions = new PostJvPreconditions(Repo<PayrollArea>(), _builder, _switch) { LocalizationManager = Localization };
        return Wire(new PostJvAppService(preconditions, screen, _runLog, _jobs));
    }

    private CostingFileAppService CreateCostingFile(params int[] areaEmployeeIds)
    {
        var screen = CreateScreen(areaEmployeeIds);
        return Wire(new CostingFileAppService(_builder, screen, _runLog, _jobs));
    }

    private static CostingRunInputDto Dto(string fingerprint = null, PayrollRunType runType = PayrollRunType.Regular) => new()
    {
        PayrollAreaName = AreaName,
        PayInDate = PayInDate,
        PostingDate = PostingDate,
        PayrollRunType = runType,
        PreviewFingerprint = fingerprint
    };

    private string PreviewOf(JournalVoucherResult voucher) => JournalVoucherFingerprint.Compute(Input(), voucher);

    private void BuilderReturns(JournalVoucherResult voucher) =>
        _builder.BuildAsync(Arg.Any<CostingRunInput>()).Returns(voucher);

    private Task NothingQueued() =>
        _jobs.DidNotReceiveWithAnyArgs().EnqueueAsync<PostJvBackgroundJob, FinanceRunJobArgs>(default);

    // ---------------------------------------------------------------- Post JV refusals

    [Fact]
    public async Task Should_Refuse_To_Post_When_Dynamics_365_Is_Switched_Off()
    {
        SeedArea();
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(voucher);
        _switch.IsPostingAllowedAsync().Returns(false);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => CreatePostJv().PostJournalVoucher(Dto(PreviewOf(voucher))));

        ex.Message.ShouldBe(Text("D365FinancePostingSwitchedOff"));
        await NothingQueued();
        Context.ExecutionLogs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_To_Post_A_Stale_Preview()
    {
        SeedArea();
        var previewed = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(Voucher(Line("E1", 12, 0), Line("", 0, 12)));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => CreatePostJv().PostJournalVoucher(Dto(PreviewOf(previewed))));

        ex.Message.ShouldBe(Text("JournalVoucherPreviewChanged"));
        await NothingQueued();
    }

    [Fact]
    public async Task Should_Refuse_To_Post_Without_A_Preview()
    {
        SeedArea();
        BuilderReturns(Voucher(Line("E1", 10, 0), Line("", 0, 10)));

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => CreatePostJv().PostJournalVoucher(Dto()));

        ex.Message.ShouldBe(Text("JournalVoucherPreviewRequired"));
        await NothingQueued();
    }

    /// <summary>The gate used to be the browser's disabled button only.</summary>
    [Fact]
    public async Task Should_Refuse_To_Post_An_Unbalanced_Jv_On_The_Server()
    {
        SeedArea();
        var voucher = Voucher(Line("E1", 10.6, 0), Line("", 0, 10.4));
        BuilderReturns(voucher);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => CreatePostJv().PostJournalVoucher(Dto(PreviewOf(voucher))));

        ex.Message.ShouldBe(Text("DebitCreditNotMatched"));
        await NothingQueued();
    }

    [Fact]
    public async Task Should_Refuse_To_Post_For_An_Area_Without_Its_Fi_Links()
    {
        SeedArea(withFiLinks: false);
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(voucher);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => CreatePostJv().PostJournalVoucher(Dto(PreviewOf(voucher))));

        ex.Message.ShouldBe(Text("PayrollFICompanyNull"));
        await NothingQueued();
    }

    // ---------------------------------------------------------------- Queuing

    [Fact]
    public async Task Should_Queue_The_Posting_With_The_Previewed_Fingerprint()
    {
        SeedArea();
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(voucher);
        var fingerprint = PreviewOf(voucher);

        var started = await CreatePostJv().PostJournalVoucher(Dto(fingerprint));

        var record = Context.ExecutionLogs.Single();
        started.ExecutionLogId.ShouldBe(record.Id);
        record.ExecutionStatus.ShouldBe(ExecutionLogStatus.INPRECESS);
        record.ScreenName.ShouldBe(FinanceRunLog.PostJvScreenName);

        await _jobs.Received(1).EnqueueAsync<PostJvBackgroundJob, FinanceRunJobArgs>(
            Arg.Is<FinanceRunJobArgs>(a => a.ExecutionLogId == record.Id
                                           && a.PreviewFingerprint == fingerprint
                                           && a.PayInDate == PayInDate
                                           && a.TenantId == TenantId),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task Should_Not_Start_A_Second_Run_While_One_Is_Going()
    {
        SeedArea();
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(voucher);
        var postJv = CreatePostJv();
        await postJv.PostJournalVoucher(Dto(PreviewOf(voucher)));

        await Should.ThrowAsync<UserFriendlyException>(() => postJv.PostJournalVoucher(Dto(PreviewOf(voucher))));

        Context.ExecutionLogs.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Queue_The_Cost_Engine_And_Empty_The_Log_First()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        Context.ProjectLogs.Add(new HCMv3.ProjectManagement.ProjectLog
        {
            TenantId = TenantId, EmployeeId = e1, Message = "old", RunTimeStamp = DateTime.Now, InPeriod = PayInDate
        });
        Context.SaveChanges();

        var started = await CreateCostingFile().GenerateCostingFile(Dto());

        LogRows().ShouldBeEmpty();
        started.Message.ShouldBe(Text("CostingFileRunQueued"));
        await _jobs.Received(1).EnqueueAsync<CostingFileBackgroundJob, FinanceRunJobArgs>(
            Arg.Is<FinanceRunJobArgs>(a => a.ExecutionLogId == started.ExecutionLogId),
            Arg.Any<BackgroundJobPriority>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task Should_Return_The_Preview_With_Its_Fingerprint()
    {
        SeedArea();
        var voucher = Voucher(Line("E1", 10, 0), Line("", 0, 10));
        BuilderReturns(voucher);

        var preview = await CreatePostJv().GetJournalVoucherLines(Dto());

        preview.Lines.Count.ShouldBe(2);
        preview.PreviewFingerprint.ShouldBe(PreviewOf(voucher));
    }

    // ---------------------------------------------------------------- Period (legacy SetCurrentDates / SetDates)

    [Fact]
    public async Task Should_Fill_The_Current_Period_From_The_Control_Record_And_Default_The_Posting_Date_To_Its_End()
    {
        SeedArea();
        Context.PayrollControlRecords.Add(new PayrollControlRecord
        {
            TenantId = TenantId, PayrollAreaId = PayrollAreaId, PayrollMonth = 9, PayrollYear = 2026,
            CalendarMonthStart = new DateTime(2026, 9, 1), CalendarMonthEnd = new DateTime(2026, 9, 30)
        });
        Context.SaveChanges();

        var context = await CreateCostingFile().GetCostingPeriodContext(AreaName);

        context.HasControlRecord.ShouldBeTrue();
        context.PayrollMonth.ShouldBe(9);
        context.CalendarMonthStart.ShouldBe(new DateTime(2026, 9, 1));
        context.CalendarMonthEnd.ShouldBe(new DateTime(2026, 9, 30));
        context.DefaultPostingDate.ShouldBe(new DateTime(2026, 9, 30));
    }

    [Fact]
    public async Task Should_Say_So_When_The_Area_Has_No_Control_Record()
    {
        SeedArea();

        var context = await CreatePostJv().GetCostingPeriodContext(AreaName);

        context.HasControlRecord.ShouldBeFalse();
        context.Message.ShouldBe(Text("PayrollRunControlRecordIsEmpty"));
    }

    [Fact]
    public async Task Should_Work_Out_An_Other_Period_From_The_Payroll_Period()
    {
        SeedArea();

        var dates = await CreateCostingFile().GetCostingPeriodDates(AreaName, 2, 2026);

        dates.CalendarMonthStart.ShouldBe(new DateTime(2026, 2, 1));
        dates.CalendarMonthEnd.ShouldBe(new DateTime(2026, 2, 28));
    }

    // ---------------------------------------------------------------- Employee popup (legacy BuildEmployeePopupList)

    [Fact]
    public async Task Should_List_Active_Area_Employees_As_Processed_Or_Pending_For_A_Regular_Run()
    {
        SeedArea();
        var processed = AddEmployee("E1");
        var pending = AddEmployee("E2");
        var inactive = AddEmployee("E3", EmployeeStatus.Inactive);
        AddCostRegister(processed, PayrollRunType.Regular);

        var output = await CreateCostingFile(processed, pending, inactive).GetCostingFileCandidates(new GetCostingFileCandidatesInput
        {
            PayrollAreaName = AreaName, PayrollRunType = PayrollRunType.Regular, PayInDate = PayInDate,
            SelectedEmployeeCodes = "e2", MaxResultCount = 10
        });

        output.AllCount.ShouldBe(2);
        output.ProcessedCount.ShouldBe(1);
        output.PendingCount.ShouldBe(1);
        output.SelectedCount.ShouldBe(1);
        output.Items.Single(i => i.EmployeeCode == "E1").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Processed);
        output.Items.Single(i => i.EmployeeCode == "E2").IsSelected.ShouldBeTrue();
    }

    /// <summary>LEGACY BEHAVIOUR, PINNED: the popup's off-cycle branch is commented out, so it lists nobody.</summary>
    [Fact]
    public async Task Should_List_Nobody_For_An_OffCycle_Run_As_Legacy_Did()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        AddCostRegister(e1, PayrollRunType.OffCycle);

        var output = await CreateCostingFile(e1).GetCostingFileCandidates(new GetCostingFileCandidatesInput
        {
            PayrollAreaName = AreaName, PayrollRunType = PayrollRunType.OffCycle, PayInDate = PayInDate, MaxResultCount = 10
        });

        output.AllCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Every_Code_The_Filter_Shows_For_Select_All()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        AddCostRegister(e1, PayrollRunType.Regular);

        var codes = await CreatePostJv(e1, e2).GetCostingFileCandidateCodes(new GetCostingFileCandidatesInput
        {
            PayrollAreaName = AreaName, PayrollRunType = PayrollRunType.Regular, PayInDate = PayInDate,
            StatusFilter = PayrollRunCandidateFilter.Pending
        });

        codes.ShouldBe(new[] { "E2" });
    }
}
