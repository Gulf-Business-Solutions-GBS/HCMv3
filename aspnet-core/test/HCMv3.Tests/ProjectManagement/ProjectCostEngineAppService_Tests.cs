using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Dto;
using HCMv3.ProjectManagement.Exporting;
using HCMv3.ProjectManagement.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Unit tests for <see cref="ProjectCostEngineAppService"/> -- the operator surface ported from legacy
/// <c>uscProjectCosting</c> (July-August 2026). The app service is constructed directly (no ABP DI/interceptor
/// pipeline), so <c>[AbpAuthorize]</c> is checked by reflection at the end rather than exercised.
/// Localization keys come back as themselves.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProjectCostEngineAppService_Tests
{
    private const string Area = "PA";
    private static readonly DateTime January = new DateTime(2024, 1, 31);

    private readonly List<ProjectLog> _projectLogs = new List<ProjectLog>();
    private readonly List<Employee> _employees = new List<Employee>();
    private readonly List<PayrollControlRecord> _controlRecords = new List<PayrollControlRecord>();
    private readonly List<PayrollArea> _payrollAreas = new List<PayrollArea>();
    private readonly List<ProjectCostEngineCandidate> _candidates = new List<ProjectCostEngineCandidate>();

    private readonly IBackgroundJobManager _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
    private readonly IEngineRunProgressWriter _progressWriter = Substitute.For<IEngineRunProgressWriter>();
    private readonly IProjectCostEngineCandidateSelector _candidateSelector = Substitute.For<IProjectCostEngineCandidateSelector>();
    private readonly IProjectCostEngineLogExcelExporter _exporter = Substitute.For<IProjectCostEngineLogExcelExporter>();
    private readonly EngineRunProgressReader _progressReader = Substitute.For<EngineRunProgressReader>(
        Substitute.For<IRepository<EngineRun, long>>(),
        Substitute.For<IRepository<EngineRunEmployee, long>>());

    public ProjectCostEngineAppService_Tests()
    {
        _candidateSelector.GetCandidatesAsync(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(_ => Task.FromResult(_candidates.ToList()));
    }

    private ProjectCostEngineAppService CreateService()
    {
        var service = new ProjectCostEngineAppService(
            _backgroundJobManager,
            Fakes.RepoOver(_projectLogs),
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_controlRecords),
            Fakes.RepoOver(_payrollAreas),
            _progressReader,
            _progressWriter,
            _candidateSelector,
            _exporter);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    private static RunProjectCostEngineInput ValidInput()
    {
        return new RunProjectCostEngineInput
        {
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = January,
            PayrollAreaName = Area
        };
    }

    private Task ReceivedEnqueue(Func<ProjectCostEngineJobArgs, bool> predicate, int times = 1)
    {
        return _backgroundJobManager.Received(times)
            .EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
                Arg.Is<ProjectCostEngineJobArgs>(a => predicate(a)),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>());
    }

    private Task DidNotEnqueue()
    {
        return _backgroundJobManager.DidNotReceive()
            .EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
                Arg.Any<ProjectCostEngineJobArgs>(),
                Arg.Any<BackgroundJobPriority>(),
                Arg.Any<TimeSpan?>());
    }

    #region << Run >>

    [Fact]
    public async Task Should_Queue_A_Run_For_Valid_Input_As_The_Current_User()
    {
        var input = ValidInput();
        input.EmployeeCodes = "E1,E2";
        input.PayInDate = new DateTime(2024, 1, 31, 15, 30, 0);

        var result = await CreateService().RunProjectCostEngine(input);

        result.Queued.ShouldBeTrue();
        await ReceivedEnqueue(a => a.TenantId == 1
                                   && a.UserId == 1
                                   && a.PayrollRunType == PayrollRunType.Regular
                                   && a.EmployeeCodes == "E1,E2"
                                   && a.PayInDate == January
                                   && a.PayrollAreaName == Area);
    }

    /// <summary>Legacy line 116: neither payroll area nor employee code entered.</summary>
    [Fact]
    public async Task Should_Reject_When_Neither_PayrollArea_Nor_EmployeeCode_Given()
    {
        var input = ValidInput();
        input.PayrollAreaName = null;

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().RunProjectCostEngine(input));

        ex.Message.ShouldBe("ProjectCostEnginePayrollAreaOrEmployeeCodeRequired");
        await DidNotEnqueue();
    }

    /// <summary>Codes alone are not enough: legacy line 134 reads the payroll area for the control record.</summary>
    [Fact]
    public async Task Should_Reject_Employee_Codes_Without_A_Payroll_Area()
    {
        var input = ValidInput();
        input.PayrollAreaName = null;
        input.EmployeeCodes = "E1";

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().RunProjectCostEngine(input));

        ex.Message.ShouldBe("ProjectCostEnginePayrollAreaRequired");
    }

    /// <summary>Legacy line 207.</summary>
    [Fact]
    public async Task Should_Reject_A_Run_With_No_Pay_Date()
    {
        var input = ValidInput();
        input.PayInDate = default(DateTime);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().RunProjectCostEngine(input));

        ex.Message.ShouldBe("ProjectCostEnginePayInDateRequired");
    }

    /// <summary>
    /// Legacy hid the run type and always ran Regular; the engine has no off-cycle arm. An off-cycle request is
    /// REFUSED, not queued as a run that would do nothing.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_An_OffCycle_Run()
    {
        var input = ValidInput();
        input.PayrollRunType = PayrollRunType.OffCycle;
        input.OffCyclePaymentDate = January;
        input.OffCycleReason = "End Of Service";

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().RunProjectCostEngine(input));

        ex.Message.ShouldBe("ProjectCostEngineOffCycleNotSupported");
        await DidNotEnqueue();
    }

    /// <summary>
    /// Legacy's off-cycle boxes were hidden: the payment date was empty and the reason the hidden combo's first item,
    /// normalised to "" (line 140). Whatever a client sends for a regular run, the engine gets exactly that.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_The_OffCycle_Values_Of_A_Regular_Run()
    {
        var input = ValidInput();
        input.OffCycleReason = "End Of Service";
        input.OffCyclePaymentDate = new DateTime(2024, 2, 15);

        await CreateService().RunProjectCostEngine(input);

        await ReceivedEnqueue(a => a.OffCycleReason == string.Empty && a.OffCyclePaymentDate == default(DateTime));
    }

    /// <summary>One run at a time: the operator is told straight away and nothing is queued.</summary>
    [Fact]
    public async Task Should_Refuse_A_Run_While_Another_Is_Going()
    {
        _progressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey)
            .Returns(new ActiveEngineRun { EngineRunId = 5, StartedByUserName = "admin", StartedAt = new DateTime(2024, 2, 1, 9, 0, 0) });

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().RunProjectCostEngine(ValidInput()));

        ex.Message.ShouldBe("ProjectCostEngineAlreadyRunning");
        await DidNotEnqueue();
    }

    #endregion

    #region << Stop / Clear / Abandon / progress >>

    /// <summary>Stop lands on the RUN (its progress row), not on the first ProjectLog row as legacy's did.</summary>
    [Fact]
    public async Task Stop_Should_Flag_The_Run_Not_A_Log_Row()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1, RunTimeStamp = DateTime.Now });

        await CreateService().StopProjectCostEngine();

        await _progressReader.Received(1).RequestStopAsync(EngineType.ProjectCostEngine);
        _projectLogs[0].StopFlag.ShouldBeFalse();
    }

    /// <summary>Legacy btnClearStatus: TruncateLog + the status reset (lines 1536-1559).</summary>
    [Fact]
    public async Task Clear_Should_Clear_The_Progress_History_And_The_Log()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1 });
        _projectLogs.Add(new ProjectLog { Id = 2 });

        await CreateService().ClearProjectCostEngineProgress();

        await _progressReader.Received(1).ClearAsync(EngineType.ProjectCostEngine);
        _projectLogs.ShouldBeEmpty();
    }

    /// <summary>A running run's log is kept: its stop checks and final counts read that table.</summary>
    [Fact]
    public async Task Clear_Should_Keep_The_Log_Of_A_Run_In_Progress()
    {
        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1 });
        _progressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey)
            .Returns(new ActiveEngineRun { EngineRunId = 5 });

        await CreateService().ClearProjectCostEngineProgress();

        await _progressReader.Received(1).ClearAsync(EngineType.ProjectCostEngine);
        _projectLogs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Abandon_Should_Age_Out_This_Engines_Stale_Runs()
    {
        _progressReader.AbandonStaleRunsAsync(EngineType.ProjectCostEngine).Returns(2);

        var count = await CreateService().AbandonStaleProjectCostEngineRuns();

        count.ShouldBe(2);
    }

    [Fact]
    public async Task Status_Should_Be_The_Shared_Progress_Of_This_Engine()
    {
        var status = new EngineRunStatusDto { HasRun = true, RunId = 9, EngineType = EngineType.ProjectCostEngine };
        _progressReader.GetStatusAsync(EngineType.ProjectCostEngine).Returns(status);

        var result = await CreateService().GetProjectCostEngineStatus();

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public async Task Run_Employees_Should_Be_Read_For_This_Engine()
    {
        var input = new GetEngineRunEmployeesInput { OnlyFailed = true, MaxResultCount = 10 };
        var page = new PagedResultDto<EngineRunEmployeeDto>(0, new List<EngineRunEmployeeDto>());
        _progressReader.GetEmployeesAsync(EngineType.ProjectCostEngine, input).Returns(page);

        var result = await CreateService().GetProjectCostEngineRunEmployees(input);

        result.ShouldBeSameAs(page);
    }

    #endregion

    #region << Period (legacy SetCurrentDates / SetDates) >>

    private void GivenArea(string payrollPeriodCode = "Jan-Dec", bool withControlRecord = true, bool locked = true)
    {
        var area = new PayrollArea
        {
            Id = 1,
            TenantId = 1,
            Code = Area,
            Name = Area,
            PayrollPeriodId = 1,
            PayrollPeriod = new PayrollPeriod { Id = 1, TenantId = 1, Code = payrollPeriodCode, NameEn = payrollPeriodCode, NameAr = payrollPeriodCode }
        };
        _payrollAreas.Add(area);

        if (withControlRecord)
        {
            _controlRecords.Add(new PayrollControlRecord
            {
                Id = 1,
                TenantId = 1,
                PayrollAreaId = 1,
                PayrollArea = area,
                PayrollMonth = 1,
                PayrollYear = 2024,
                CalendarMonthStart = new DateTime(2024, 1, 1),
                CalendarMonthEnd = January,
                PeriodLocked = locked
            });
        }
    }

    [Fact]
    public async Task Context_Should_Ask_For_A_Payroll_Area_First()
    {
        var period = await CreateService().GetProjectCostEngineContext(null);

        period.HasControlRecord.ShouldBeFalse();
        period.Message.ShouldBe("PayrollRunPleaseSelectPayrollArea");
    }

    /// <summary>Legacy null guard of 7 August 2026: no control record, nothing filled.</summary>
    [Fact]
    public async Task Context_Should_Fill_Nothing_When_The_Area_Has_No_Control_Record()
    {
        GivenArea(withControlRecord: false);

        var period = await CreateService().GetProjectCostEngineContext(Area);

        period.HasControlRecord.ShouldBeFalse();
        period.CalendarMonthEnd.ShouldBeNull();
        period.Message.ShouldBe("PayrollRunControlRecordIsEmpty");
    }

    /// <summary>Legacy SetCurrentDates: month, year, start and end from the control record.</summary>
    [Fact]
    public async Task Context_Should_Fill_The_Current_Period_From_The_Control_Record()
    {
        GivenArea();

        var period = await CreateService().GetProjectCostEngineContext(Area);

        period.HasControlRecord.ShouldBeTrue();
        period.PayrollMonth.ShouldBe(1);
        period.PayrollYear.ShouldBe(2024);
        period.CalendarMonthStart.ShouldBe(new DateTime(2024, 1, 1));
        period.CalendarMonthEnd.ShouldBe(January);
        period.PeriodLocked.ShouldBeTrue();
        period.RunAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Context_Should_Say_The_Run_Is_Not_Allowed_While_The_Period_Is_Unlocked()
    {
        GivenArea(locked: false);

        var period = await CreateService().GetProjectCostEngineContext(Area);

        period.CalendarMonthEnd.ShouldBe(January);
        period.RunAllowed.ShouldBeFalse();
    }

    /// <summary>Legacy SetDates: the typed month/year through the area's payroll year.</summary>
    [Fact]
    public async Task Other_Period_Should_Map_Month_And_Year_Through_The_Payroll_Year()
    {
        GivenArea(payrollPeriodCode: "Feb-Jan");

        // Feb-Jan: payroll month 1 of 2024 is February 2024.
        var period = await CreateService().GetProjectCostEnginePeriodDates(Area, 1, 2024);

        period.CalendarMonthStart.ShouldBe(new DateTime(2024, 2, 1));
        period.CalendarMonthEnd.ShouldBe(new DateTime(2024, 2, 29));
        period.PayrollMonth.ShouldBe(1);
        period.PayrollYear.ShouldBe(2024);
        period.HasControlRecord.ShouldBeTrue();
        // Not the control record's own period end, so the engine would refuse it (legacy Main line 235).
        period.RunAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task Other_Period_Equal_To_The_Locked_Control_Record_Should_Be_Runnable()
    {
        GivenArea();

        var period = await CreateService().GetProjectCostEnginePeriodDates(Area, 1, 2024);

        period.CalendarMonthEnd.ShouldBe(January);
        period.RunAllowed.ShouldBeTrue();
    }

    /// <summary>Legacy swallowed the error and left the dates alone; here they come back empty, with the reason.</summary>
    [Fact]
    public async Task Other_Period_Should_Give_No_Dates_For_An_Unknown_Payroll_Year()
    {
        GivenArea(payrollPeriodCode: "Whatever");

        var period = await CreateService().GetProjectCostEnginePeriodDates(Area, 1, 2024);

        period.CalendarMonthStart.ShouldBeNull();
        period.CalendarMonthEnd.ShouldBeNull();
        period.Message.ShouldBe("IncorrectPayrollPeriod");
    }

    [Fact]
    public async Task Other_Period_Should_Give_No_Dates_For_A_Month_Out_Of_Range()
    {
        GivenArea();

        var period = await CreateService().GetProjectCostEnginePeriodDates(Area, 13, 2024);

        period.CalendarMonthEnd.ShouldBeNull();
        period.Message.ShouldBe("IncorrectPayrollPeriod");
    }

    #endregion

    #region << Employee picker >>

    private void GivenCandidates()
    {
        _candidates.Add(new ProjectCostEngineCandidate { EmployeeId = 1, EmployeeCode = "E1", FullNameEn = "Ali", FullNameAr = "علي", IsProcessed = true });
        _candidates.Add(new ProjectCostEngineCandidate { EmployeeId = 2, EmployeeCode = "E2", FullNameEn = "Sara", IsProcessed = false });
        _candidates.Add(new ProjectCostEngineCandidate { EmployeeId = 3, EmployeeCode = "E3", FullNameEn = "Omar", IsProcessed = false });
    }

    private static GetProjectCostEngineCandidatesInput CandidatesInput(
        PayrollRunCandidateFilter filter = PayrollRunCandidateFilter.All,
        string selected = null,
        string search = null)
    {
        return new GetProjectCostEngineCandidatesInput
        {
            PayrollAreaName = Area,
            PayrollDate = January,
            StatusFilter = filter,
            SelectedEmployeeCodes = selected,
            Filter = search,
            MaxResultCount = 10
        };
    }

    /// <summary>Legacy UpdateFilterCounts: the four counts are over the whole list, whatever is filtered.</summary>
    [Fact]
    public async Task Candidates_Should_Count_All_Processed_Pending_And_Selected()
    {
        GivenCandidates();

        var result = await CreateService().GetProjectCostEngineCandidates(
            CandidatesInput(PayrollRunCandidateFilter.Pending, selected: " e1 ,E3"));

        result.AllCount.ShouldBe(3);
        result.ProcessedCount.ShouldBe(1);
        result.PendingCount.ShouldBe(2);
        result.SelectedCount.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Items.Select(c => c.EmployeeCode).ShouldBe(new[] { "E2", "E3" });
        result.Items.Single(c => c.EmployeeCode == "E3").IsSelected.ShouldBeTrue();
        result.PayrollDate.ShouldBe(January);
        await _candidateSelector.Received().GetCandidatesAsync(Area, January);
    }

    [Fact]
    public async Task Candidates_Should_Show_Processed_Or_Pending_Status()
    {
        GivenCandidates();

        var result = await CreateService().GetProjectCostEngineCandidates(CandidatesInput());

        result.Items.Single(c => c.EmployeeCode == "E1").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Processed);
        result.Items.Single(c => c.EmployeeCode == "E2").PayrollStatus.ShouldBe(PayrollRunCandidateStatus.Pending);
    }

    [Fact]
    public async Task Candidates_Should_Filter_To_The_Selected_Ones()
    {
        GivenCandidates();

        var result = await CreateService().GetProjectCostEngineCandidates(
            CandidatesInput(PayrollRunCandidateFilter.Selected, selected: "E2"));

        result.Items.Select(c => c.EmployeeCode).ShouldBe(new[] { "E2" });
    }

    [Fact]
    public async Task Candidates_Should_Search_Code_And_Name()
    {
        GivenCandidates();

        var result = await CreateService().GetProjectCostEngineCandidates(CandidatesInput(search: "sar"));

        result.Items.Select(c => c.EmployeeCode).ShouldBe(new[] { "E2" });
    }

    [Fact]
    public async Task Candidates_Should_Page_And_Sort()
    {
        GivenCandidates();
        var input = CandidatesInput();
        input.Sorting = "EmployeeCode DESC";
        input.MaxResultCount = 2;
        input.SkipCount = 1;

        var result = await CreateService().GetProjectCostEngineCandidates(input);

        result.TotalCount.ShouldBe(3);
        result.Items.Select(c => c.EmployeeCode).ShouldBe(new[] { "E2", "E1" });
    }

    [Fact]
    public async Task Candidates_Should_Be_Empty_Without_A_Period_End()
    {
        GivenCandidates();
        var input = CandidatesInput();
        input.PayrollDate = default(DateTime);

        var result = await CreateService().GetProjectCostEngineCandidates(input);

        result.AllCount.ShouldBe(0);
        await _candidateSelector.DidNotReceive().GetCandidatesAsync(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Candidates_Should_Show_The_Arabic_Name_Under_An_Arabic_UI()
    {
        GivenCandidates();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-KW");

            var result = await CreateService().GetProjectCostEngineCandidates(CandidatesInput());

            result.Items.Single(c => c.EmployeeCode == "E1").EmployeeName.ShouldBe("علي");
            // No Arabic name: the English one.
            result.Items.Single(c => c.EmployeeCode == "E2").EmployeeName.ShouldBe("Sara");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>Legacy SelectAllEmployees / OK: every code the filter shows, on every page.</summary>
    [Fact]
    public async Task Candidate_Codes_Should_Be_Every_Code_The_Filter_Shows()
    {
        GivenCandidates();
        var input = CandidatesInput(PayrollRunCandidateFilter.Pending);
        input.MaxResultCount = 1;

        var codes = await CreateService().GetProjectCostEngineCandidateCodes(input);

        codes.ShouldBe(new List<string> { "E2", "E3" });
    }

    #endregion

    #region << Log grid + export >>

    private void GivenLogs()
    {
        _employees.Add(new Employee { Id = 1, Code = "E1", FullNameEn = "Ali", FullNameAr = "علي" });
        _employees.Add(new Employee { Id = 2, Code = "E2", FullNameEn = "Sara" });

        _projectLogs.Add(new ProjectLog { Id = 1, EmployeeId = 1, Status = 0, Message = "Successfull", InPeriod = January, RunTimeStamp = new DateTime(2024, 2, 1, 9, 0, 0) });
        _projectLogs.Add(new ProjectLog { Id = 2, EmployeeId = 2, Status = 1, Message = "Salary Register not found", InPeriod = January, RunTimeStamp = new DateTime(2024, 2, 1, 9, 1, 0) });
        // The run-level failure row names no employee.
        _projectLogs.Add(new ProjectLog { Id = 3, EmployeeId = null, Status = 1, Message = "Payroll Calender does not have record", InPeriod = January, RunTimeStamp = new DateTime(2024, 2, 1, 9, 2, 0) });
        // Legacy shows Status 0 and 1 only.
        _projectLogs.Add(new ProjectLog { Id = 4, EmployeeId = 1, Status = 7, Message = "not a log status" });
    }

    [Fact]
    public async Task Logs_Should_List_The_Run_Log_With_Its_Counts()
    {
        GivenLogs();

        var result = await CreateService().GetProjectCostEngineLogs(new GetProjectCostEngineLogsInput { MaxResultCount = 10 });

        result.AllCount.ShouldBe(3);
        result.SuccessCount.ShouldBe(1);
        result.FailedCount.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
        result.Items.Select(l => l.Id).ShouldBe(new[] { 1, 2, 3 });

        var first = result.Items[0];
        first.EmployeeCode.ShouldBe("E1");
        first.EmployeeName.ShouldBe("Ali");
        first.Status.ShouldBe(0);
        first.InPeriod.ShouldBe(January);
        first.RunTimeStamp.ShouldBe(new DateTime(2024, 2, 1, 9, 0, 0));

        var runLevel = result.Items[2];
        runLevel.EmployeeCode.ShouldBe(string.Empty);
        runLevel.EmployeeName.ShouldBe(string.Empty);
        runLevel.Message.ShouldBe("Payroll Calender does not have record");
    }

    [Fact]
    public async Task Logs_Should_Filter_Failed_Or_Successful()
    {
        GivenLogs();
        var service = CreateService();

        var failed = await service.GetProjectCostEngineLogs(new GetProjectCostEngineLogsInput { OnlyFailed = true, MaxResultCount = 10 });
        var successful = await service.GetProjectCostEngineLogs(new GetProjectCostEngineLogsInput { OnlyFailed = false, MaxResultCount = 10 });

        failed.Items.Select(l => l.Id).ShouldBe(new[] { 2, 3 });
        failed.AllCount.ShouldBe(3);
        successful.Items.Select(l => l.Id).ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Logs_Should_Sort_By_The_Grid_Column()
    {
        GivenLogs();

        var result = await CreateService().GetProjectCostEngineLogs(
            new GetProjectCostEngineLogsInput { Sorting = "runTimeStamp desc", MaxResultCount = 10 });

        result.Items.Select(l => l.Id).ShouldBe(new[] { 3, 2, 1 });
    }

    /// <summary>Legacy btnExportTimeLog_Click: the grid as filtered, every page.</summary>
    [Fact]
    public async Task Logs_Export_Should_Hand_The_Filtered_Log_To_The_Exporter()
    {
        GivenLogs();
        var file = new FileDto("ProjectCostEngineExecutionLog_x.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        _exporter.ExportToFile(Arg.Any<List<ProjectCostEngineLogDto>>()).Returns(file);

        var result = await CreateService().GetProjectCostEngineLogsToExcel(new GetProjectCostEngineLogsToExcelInput { OnlyFailed = true });

        result.ShouldBeSameAs(file);
        await _exporter.Received(1).ExportToFile(Arg.Is<List<ProjectCostEngineLogDto>>(l =>
            l.Count == 2 && l[0].Id == 2 && l[0].EmployeeCode == "E2" && l[1].Id == 3));
    }

    #endregion

    #region << Endpoint shape: permissions and verbs >>

    /// <summary>
    /// Every read is gated on the Project Cost Engine VIEW permission -- never on Project Logs or Payroll Engine --
    /// and every change on its Run permission.
    /// </summary>
    [Theory]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineStatus), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineRunEmployees), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineContext), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEnginePeriodDates), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineCandidates), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineCandidateCodes), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineLogs), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineLogsToExcel), AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    [InlineData(nameof(ProjectCostEngineAppService.RunProjectCostEngine), AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    [InlineData(nameof(ProjectCostEngineAppService.StopProjectCostEngine), AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    [InlineData(nameof(ProjectCostEngineAppService.ClearProjectCostEngineProgress), AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    [InlineData(nameof(ProjectCostEngineAppService.AbandonStaleProjectCostEngineRuns), AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public void Should_Gate_Each_Endpoint_On_The_Project_Cost_Engine_Permission(string methodName, string permission)
    {
        var method = typeof(ProjectCostEngineAppService).GetMethod(methodName);

        method.ShouldNotBeNull();
        var authorize = method.GetCustomAttributes<AbpAuthorizeAttribute>().ToList();
        authorize.Count.ShouldBe(1);
        authorize[0].Permissions.ShouldBe(new[] { permission });
    }

    /// <summary>The repo's NSwag rule: Get* methods with a custom input DTO must be POST, or no DTO is generated.</summary>
    [Theory]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineRunEmployees))]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineCandidates))]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineCandidateCodes))]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineLogs))]
    [InlineData(nameof(ProjectCostEngineAppService.GetProjectCostEngineLogsToExcel))]
    [InlineData(nameof(ProjectCostEngineAppService.RunProjectCostEngine))]
    public void Should_Post_Methods_That_Take_A_Custom_Input(string methodName)
    {
        typeof(ProjectCostEngineAppService).GetMethod(methodName).GetCustomAttribute<HttpPostAttribute>().ShouldNotBeNull();
    }

    #endregion
}
