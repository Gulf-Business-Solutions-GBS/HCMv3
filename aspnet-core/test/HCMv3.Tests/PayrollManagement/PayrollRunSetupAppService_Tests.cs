using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using ClosedXML.Excel;
using HCMv3.Dto;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Exporting;
using HCMv3.PayrollManagement.Services;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The Payroll Engine screen's guidance service (legacy <c>uscPayrollEngine</c> SetCurrentDates, BindOffReasonType,
/// BuildEmployeePopupList + filters, btnExportPayrollLog_Click). Control records, areas and reason types live in the
/// module's Sqlite test context; who is eligible is <see cref="PayrollRunCandidateSelector_Tests"/>'s business, so the
/// selector is substituted here.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRunSetupAppService_Tests : PayrollManagementAppServiceTestBase
{
    private const string Area = "KWT";

    private readonly IPayrollRunCandidateSelector _selector = Substitute.For<IPayrollRunCandidateSelector>();
    private readonly ISysSettingsAccessor _settings = Substitute.For<ISysSettingsAccessor>();
    private readonly IPayrollRunEmployeesExcelExporter _exporter = Substitute.For<IPayrollRunEmployeesExcelExporter>();
    private readonly EngineRunProgressReader _progressReader = Substitute.For<EngineRunProgressReader>(
        Substitute.For<IRepository<EngineRun, long>>(),
        Substitute.For<IRepository<EngineRunEmployee, long>>());

    private readonly ManageTimeSettings _timeSettings = new() { EnableTimeEngineAutorun = false };
    private readonly ManagePayrollSettings _payrollSettings = new() { PickOffcyclePayment = PickOffcyclePayment.CurrentMonth };

    private List<PayrollRunCandidate> _candidates = new();

    public PayrollRunSetupAppService_Tests()
    {
        _settings.GetTimeSettingsAsync().Returns(_ => Task.FromResult(_timeSettings));
        _settings.GetPayrollSettingsAsync().Returns(_ => Task.FromResult(_payrollSettings));

        _selector.GetCandidatesAsync(
                Arg.Any<string>(),
                Arg.Any<PayrollRunType>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<PickOffcyclePayment>(),
                Arg.Any<DateTime>())
            .Returns(_ => Task.FromResult(_candidates.ToList()));
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private PayrollRunSetupAppService CreateAppService()
    {
        var service = CreateService(() => new PayrollRunSetupAppService(
            FakeRepository.For<PayrollControlRecord>(Context),
            FakeRepository.For<PayrollArea>(Context),
            FakeRepository.For<PayrollReasonType>(Context),
            _selector,
            _settings,
            _progressReader,
            _exporter));

        // Keys come back as themselves, so the messages can be asserted exactly.
        service.LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    private PayrollArea CreatePayrollArea(string name = Area)
    {
        var period = new PayrollPeriod { TenantId = TenantId, Code = "Jan-Dec", NameEn = "Jan-Dec", NameAr = "Jan-Dec" };
        Context.PayrollPeriods.Add(period);

        var currency = new HCMv3.GeneralMasterInfo.Currency { TenantId = TenantId, Code = name, NameEn = name, NameAr = name };
        Context.Currencies.Add(currency);
        Context.SaveChanges();

        var area = new PayrollArea { TenantId = TenantId, Code = name, Name = name, PayrollPeriodId = period.Id, CurrencyId = currency.Id };
        Context.PayrollAreas.Add(area);
        Context.SaveChanges();
        return area;
    }

    private void CreateControlRecord(bool locked, bool closed, string areaName = Area)
    {
        var area = CreatePayrollArea(areaName);
        Context.PayrollControlRecords.Add(new PayrollControlRecord
        {
            TenantId = TenantId,
            PayrollAreaId = area.Id,
            PayrollMonth = 9,
            PayrollYear = 2026,
            CalendarMonthStart = new DateTime(2026, 9, 1),
            CalendarMonthEnd = new DateTime(2026, 9, 30),
            PeriodLocked = locked,
            PeriodClosed = closed
        });
        Context.SaveChanges();
    }

    private static PayrollRunCandidate Candidate(int id, bool processed) => new()
    {
        EmployeeId = id,
        EmployeeCode = "E" + id,
        FullNameEn = "Employee " + id,
        FullNameAr = "Arabic " + id,
        IsProcessed = processed
    };

    private static GetPayrollRunCandidatesInput CandidatesInput(
        PayrollRunCandidateFilter statusFilter = PayrollRunCandidateFilter.All,
        string selected = null,
        string filter = null,
        int maxResultCount = 100) => new()
    {
        PayrollAreaName = Area,
        PayrollRunType = PayrollRunType.Regular,
        StatusFilter = statusFilter,
        SelectedEmployeeCodes = selected,
        Filter = filter,
        MaxResultCount = maxResultCount,
        SkipCount = 0
    };

    #region << GetPayrollRunContext >>

    [Fact]
    public async Task Should_Suggest_A_Regular_Run_On_The_Period_End_When_The_Period_Is_Locked()
    {
        CreateControlRecord(locked: true, closed: false);

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.HasControlRecord.ShouldBeTrue();
        context.PayrollMonth.ShouldBe(9);
        context.PayrollYear.ShouldBe(2026);
        context.CalendarMonthStart.ShouldBe(new DateTime(2026, 9, 1));
        context.CalendarMonthEnd.ShouldBe(new DateTime(2026, 9, 30));
        context.PeriodStatus.ShouldBe(PayrollRunPeriodStatus.Regular);
        context.PeriodStatusText.ShouldBe("PayrollRunStatusRegular");
        context.SuggestedRunType.ShouldBe(PayrollRunType.Regular);
        context.RegularRunAllowed.ShouldBeTrue();
        context.OffCycleRunAllowed.ShouldBeFalse();
        context.RegularPayInDate.ShouldBe(new DateTime(2026, 9, 30));
    }

    [Fact]
    public async Task Should_Suggest_An_Off_Cycle_In_The_Next_Period_When_The_Period_Is_Closed()
    {
        CreateControlRecord(locked: false, closed: true);

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.PeriodStatus.ShouldBe(PayrollRunPeriodStatus.OffCycle);
        context.PeriodStatusText.ShouldBe("PayrollRunStatusOffCycle");
        context.SuggestedRunType.ShouldBe(PayrollRunType.OffCycle);
        context.RegularRunAllowed.ShouldBeFalse();
        context.OffCycleRunAllowed.ShouldBeTrue();
        context.OffCyclePayInDate.ShouldBe(new DateTime(2026, 10, 31));
        context.OffCyclePaymentDateFrom.ShouldBe(new DateTime(2026, 10, 1));
        context.OffCyclePaymentDateTo.ShouldBe(new DateTime(2026, 10, 31));
    }

    [Fact]
    public async Task Should_Suggest_No_Run_Type_While_The_Period_Is_Open_For_Data_Correction()
    {
        CreateControlRecord(locked: false, closed: false);

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.PeriodStatus.ShouldBe(PayrollRunPeriodStatus.DataCorrection);
        context.PeriodStatusText.ShouldBe("PayrollRunStatusDataCorrection");
        context.SuggestedRunType.ShouldBeNull();
        context.RegularRunAllowed.ShouldBeFalse();
        context.OffCycleRunAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Say_The_Control_Record_Is_Empty_When_The_Area_Has_None()
    {
        CreatePayrollArea();

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.HasControlRecord.ShouldBeFalse();
        context.PeriodStatus.ShouldBe(PayrollRunPeriodStatus.NoControlRecord);
        context.Message.ShouldBe("PayrollRunControlRecordIsEmpty");
        context.CalendarMonthEnd.ShouldBeNull();
        context.SuggestedRunType.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Ask_To_Select_A_Payroll_Area_When_None_Is_Given()
    {
        var context = await CreateAppService().GetPayrollRunContext(" ");

        context.HasControlRecord.ShouldBeFalse();
        context.Message.ShouldBe("PayrollRunPleaseSelectPayrollArea");
    }

    [Fact]
    public async Task Should_Hide_The_Off_Cycle_Payment_Date_When_Time_Engine_Autorun_Is_On()
    {
        CreateControlRecord(locked: false, closed: true);
        _timeSettings.EnableTimeEngineAutorun = true;

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.EnableTimeEngineAutorun.ShouldBeTrue();
        context.ShowOffCyclePaymentDate.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Show_The_Off_Cycle_Payment_Date_When_Time_Engine_Autorun_Is_Off()
    {
        CreateControlRecord(locked: false, closed: true);

        var context = await CreateAppService().GetPayrollRunContext(Area);

        context.ShowOffCyclePaymentDate.ShouldBeTrue();
        context.DefaultOffCyclePaymentDate.ShouldBe(new DateTime(2026, 10, 31));
    }

    #endregion

    #region << GetPayrollRunPeriodDates >>

    [Fact]
    public async Task Should_Return_The_Calendar_Month_Of_Another_Payroll_Month()
    {
        CreatePayrollArea();

        var dates = await CreateAppService().GetPayrollRunPeriodDates(Area, 2, 2028);

        dates.CalendarMonthStart.ShouldBe(new DateTime(2028, 2, 1));
        dates.CalendarMonthEnd.ShouldBe(new DateTime(2028, 2, 29));
    }

    [Fact]
    public async Task Should_Reject_Period_Dates_For_An_Unknown_Area()
    {
        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(
            () => CreateAppService().GetPayrollRunPeriodDates("NOPE", 2, 2028));
    }

    #endregion

    #region << GetOffCycleReasons >>

    [Fact]
    public async Task Should_Read_Off_Cycle_Reasons_From_The_Payroll_Reason_Type_Table_With_The_English_Name_As_Value()
    {
        Context.PayrollReasonTypes.Add(new PayrollReasonType { TenantId = TenantId, Code = "EOS", NameEn = PayrollOffCycleReasons.EndOfService, NameAr = "نهاية الخدمة" });
        Context.PayrollReasonTypes.Add(new PayrollReasonType { TenantId = TenantId, Code = "BON", NameEn = "Bonus", NameAr = "مكافأة" });
        Context.SaveChanges();

        var reasons = await CreateAppService().GetOffCycleReasons();

        reasons.Count.ShouldBe(2);
        reasons[0].Value.ShouldBe(PayrollOffCycleReasons.EndOfService);
        reasons[0].IsKnownToEngine.ShouldBeTrue();
        reasons[1].Value.ShouldBe("Bonus");
        reasons[1].IsKnownToEngine.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Offer_The_Three_Engine_Reasons_When_The_Table_Is_Empty()
    {
        var reasons = await CreateAppService().GetOffCycleReasons();

        reasons.Select(r => r.Value).ShouldBe(new[]
        {
            PayrollOffCycleReasons.EndOfService,
            PayrollOffCycleReasons.AdvanceLeave,
            PayrollOffCycleReasons.LeaveEncashment
        });
        reasons.ShouldAllBe(r => r.IsKnownToEngine);
    }

    #endregion

    #region << GetPayrollRunCandidates / GetPayrollRunCandidateCodes >>

    [Fact]
    public async Task Should_Return_Processed_And_Pending_Candidates_With_Counts()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(1, true), Candidate(2, false), Candidate(3, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput(selected: "e2"));

        result.TotalCount.ShouldBe(3);
        result.AllCount.ShouldBe(3);
        result.ProcessedCount.ShouldBe(1);
        result.PendingCount.ShouldBe(2);
        result.SelectedCount.ShouldBe(1);
        result.PayrollDate.ShouldBe(new DateTime(2026, 9, 30));
        result.Items.Single(c => c.EmployeeCode == "E1").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Processed);
        result.Items.Single(c => c.EmployeeCode == "E2").IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Build_The_Candidate_List_For_The_Control_Record_Period_End()
    {
        CreateControlRecord(locked: true, closed: false);

        await CreateAppService().GetPayrollRunCandidates(CandidatesInput());

        await _selector.Received(1).GetCandidatesAsync(
            Area,
            PayrollRunType.Regular,
            null,
            new DateTime(2026, 9, 30),
            PickOffcyclePayment.CurrentMonth,
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Should_Filter_Candidates_By_Processed()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(1, true), Candidate(2, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput(PayrollRunCandidateFilter.Processed));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().EmployeeCode.ShouldBe("E1");
        result.AllCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Filter_Candidates_By_Pending()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(1, true), Candidate(2, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput(PayrollRunCandidateFilter.Pending));

        result.Items.Single().EmployeeCode.ShouldBe("E2");
    }

    [Fact]
    public async Task Should_Filter_Candidates_By_Selected()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(1, true), Candidate(2, false), Candidate(3, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(
            CandidatesInput(PayrollRunCandidateFilter.Selected, selected: "E1, E3"));

        result.Items.Select(c => c.EmployeeCode).ShouldBe(new[] { "E1", "E3" });
    }

    [Fact]
    public async Task Should_Search_Candidates_By_Code_Or_Name()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(1, false), Candidate(22, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput(filter: "employee 22"));

        result.Items.Single().EmployeeCode.ShouldBe("E22");
    }

    [Fact]
    public async Task Should_Page_Candidates_But_Count_The_Whole_List()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = Enumerable.Range(1, 5).Select(i => Candidate(i, false)).ToList();

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput(maxResultCount: 2));

        result.TotalCount.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Return_No_Candidates_When_The_Area_Has_No_Control_Record()
    {
        CreatePayrollArea();
        _candidates = new List<PayrollRunCandidate> { Candidate(1, false) };

        var result = await CreateAppService().GetPayrollRunCandidates(CandidatesInput());

        result.TotalCount.ShouldBe(0);
        result.PeriodStatusText.ShouldBe("PayrollRunControlRecordIsEmpty");
        await _selector.DidNotReceiveWithAnyArgs().GetCandidatesAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task Should_Use_The_Other_Period_End_For_A_Simulation_On_Another_Month()
    {
        CreateControlRecord(locked: true, closed: false);
        var input = CandidatesInput();
        input.SimulationMonth = 3;
        input.SimulationYear = 2026;

        await CreateAppService().GetPayrollRunCandidates(input);

        await _selector.Received(1).GetCandidatesAsync(
            Area, PayrollRunType.Regular, null, new DateTime(2026, 3, 31), Arg.Any<PickOffcyclePayment>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Should_Pass_The_Reason_Only_For_An_Off_Cycle_Run()
    {
        CreateControlRecord(locked: false, closed: true);
        var input = CandidatesInput();
        input.PayrollRunType = PayrollRunType.OffCycle;
        input.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await CreateAppService().GetPayrollRunCandidates(input);

        await _selector.Received(1).GetCandidatesAsync(
            Area, PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave, new DateTime(2026, 9, 30),
            Arg.Any<PickOffcyclePayment>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Should_Return_Every_Matching_Code_For_Select_All()
    {
        CreateControlRecord(locked: true, closed: false);
        _candidates = new List<PayrollRunCandidate> { Candidate(3, false), Candidate(1, true), Candidate(2, false) };

        var codes = await CreateAppService().GetPayrollRunCandidateCodes(
            CandidatesInput(PayrollRunCandidateFilter.Pending, maxResultCount: 1));

        codes.ShouldBe(new[] { "E2", "E3" });
    }

    #endregion

    #region << GetPayrollRunEmployeesToExcel >>

    [Fact]
    public async Task Should_Export_Every_Row_Of_The_Run_With_The_Grid_Filters()
    {
        var rows = new List<EngineRunEmployeeDto>
        {
            new() { Id = 1, SequenceNo = 1, EmployeeCode = "E1", Succeeded = false, Message = "boom" }
        };
        _progressReader.GetEmployeesAsync(EngineType.PayrollEngine, Arg.Any<GetEngineRunEmployeesInput>())
            .Returns(Task.FromResult(new PagedResultDto<EngineRunEmployeeDto>(1, rows)));
        var file = new FileDto("PayrollExecutionLog.xlsx", "application/xlsx");
        _exporter.ExportToFile(Arg.Any<List<EngineRunEmployeeDto>>()).Returns(Task.FromResult(file));

        var result = await CreateAppService().GetPayrollRunEmployeesToExcel(
            new GetPayrollRunEmployeesToExcelInput { RunId = 7, OnlyFailed = true, Filter = "E" });

        result.ShouldBeSameAs(file);
        await _progressReader.Received(1).GetEmployeesAsync(
            EngineType.PayrollEngine,
            Arg.Is<GetEngineRunEmployeesInput>(i => i.RunId == 7
                                                    && i.OnlyFailed == true
                                                    && i.Filter == "E"
                                                    && i.SkipCount == 0
                                                    && i.MaxResultCount == int.MaxValue));
        await _exporter.Received(1).ExportToFile(Arg.Is<List<EngineRunEmployeeDto>>(l => l.Count == 1 && l[0].EmployeeCode == "E1"));
    }

    [Fact]
    public async Task Should_Write_The_Run_Employees_Into_An_Excel_File()
    {
        var tempFiles = Substitute.For<ITempFileCacheManager>();
        byte[] saved = null;
        tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => saved = call.ArgAt<byte[]>(1));
        var exporter = new PayrollRunEmployeesExcelExporter(tempFiles) { LocalizationManager = Fakes.LocalizationManager() };

        var file = await exporter.ExportToFile(new List<EngineRunEmployeeDto>
        {
            new() { SequenceNo = 1, EmployeeCode = "E1", EmployeeName = "Ali", Succeeded = true, Message = "Successfull", CompletedAt = new DateTime(2026, 10, 1, 9, 0, 0) },
            new() { SequenceNo = 2, EmployeeCode = "E2", EmployeeName = "Sara", Succeeded = false, Message = "No basic pay", CompletedAt = new DateTime(2026, 10, 1, 9, 1, 0) }
        });

        file.FileName.ShouldStartWith("PayrollExecutionLog_");
        file.FileName.ShouldEndWith(".xlsx");
        saved.ShouldNotBeNull();

        using var workbook = new XLWorkbook(new MemoryStream(saved));
        var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString()).ToList();
        headers.ShouldContain("EmployeeCode");
        headers.ShouldContain("Message");
        sheet.Row(3).CellsUsed().Select(c => c.GetString()).ShouldContain("E2");
        sheet.Row(3).CellsUsed().Select(c => c.GetString()).ShouldContain("Failed");
    }

    #endregion
}
