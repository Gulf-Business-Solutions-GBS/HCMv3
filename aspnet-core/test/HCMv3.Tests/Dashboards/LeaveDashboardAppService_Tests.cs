using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Timing;
using ClosedXML.Excel;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Leave;
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Workflow;
using NSubstitute;
using Shouldly;
using Xunit;

// See ProjectCostEngineFakes for why a plain List<T>.AsQueryable() is a complete stand-in here.
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Leave Dashboard shows whatever <see cref="IDashboardScopeService"/> resolves for the signed-in user (that
/// rule has its own tests in <see cref="DashboardScopeService_Tests"/>), plus the filters and the export. The
/// counting rules are covered by <see cref="LeaveDashboardCalculator_Tests"/>.
///
/// The list-backed fakes do not apply the ApprovedOnly data filter, so leave used here has no workflow status
/// except in the pending-approval test.
/// </summary>
public class LeaveDashboardAppService_Tests
{
    private const int Annual = 1;

    private readonly DateTime _today = Clock.Now.Date;

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<EmployeeAbsence> _absences = new();
    private readonly List<AbsenceType> _types = new();
    private readonly List<OrganizationUnit> _units = new();

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    public LeaveDashboardAppService_Tests()
    {
        _types.Add(new AbsenceType { Id = Annual, TenantId = 1, Code = "1", NameEn = "Annual", IsActive = true, IsReturnDateRequired = true });
        _types.Add(new AbsenceType { Id = 2, TenantId = 1, Code = "9", NameEn = "Retired type", IsActive = false });

        // 1 Company > 2 Finance > 3 Payroll
        _units.Add(Unit(1, null, "Company"));
        _units.Add(Unit(2, 1, "Finance"));
        _units.Add(Unit(3, 2, "Payroll"));

        AddEmployee(1, unitId: 1);
        AddEmployee(2, unitId: 2);
        AddEmployee(3, unitId: 3);

        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private XLWorkbook OpenExport(FileDto file) => new(new MemoryStream(_savedFiles[file.FileToken]));

    private static OrganizationUnit Unit(long id, long? parentId, string name) =>
        new OrganizationUnit(1, name, parentId) { Id = id };

    private void AddEmployee(int id, long unitId)
    {
        _employees.Add(new Employee
        {
            Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Status = EmployeeStatus.Active
        });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = _today.AddYears(-1), EndDate = DateTime.MaxValue.Date
        });
    }

    private EmployeeAbsence AddLeave(int employeeId, DateTime start, DateTime end, double hours = 0, RequestStatus? status = null)
    {
        var absence = new EmployeeAbsence
        {
            Id = _absences.Count + 1, TenantId = 1, EmployeeId = employeeId, AbsenceTypeId = Annual,
            StartDate = start, EndDate = end, TotalLeaveDays = (end - start).Days + 1,
            AbsenceHours = hours, WorkflowStatus = status
        };
        _absences.Add(absence);
        return absence;
    }

    private LeaveDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var exporter = new LeaveDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() };

        var service = new LeaveDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_absences),
            Fakes.RepoOver(_types),
            unitRepository,
            _scopeService,
            exporter);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        ((ApplicationService)service).UnitOfWorkManager = Fakes.UnitOfWorkManager();
        return service;
    }

    private static LeaveDashboardInput Input(long? unitId = null) => new() { OrganizationUnitId = unitId };

    // ---------------------------------------------------------------- options

    [Fact]
    public async Task GetOptions_Should_List_Only_Active_Leave_Types()
    {
        var options = await CreateService().GetOptions();

        options.AbsenceTypes.Select(t => t.NameEn).ShouldBe(new[] { "Annual" });
    }

    // ---------------------------------------------------------------- scope

    [Fact]
    public async Task GetDashboard_Should_Show_Everyone_When_The_Scope_Is_Everyone()
    {
        AddLeave(1, _today, _today);
        AddLeave(3, _today, _today);

        var result = await CreateService().GetDashboard(Input());

        result.Scope.Reason.ShouldBe(DashboardScopeReason.Admin);
        result.Scope.EmployeesInScope.ShouldBe(3);
        result.OnLeaveToday.Select(r => r.EmployeeId).ShouldBe(new[] { 1, 3 }, ignoreOrder: true);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Only_The_Employees_In_A_Restricted_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2, 3 }, new[] { "Finance" }, includesOwnTeam: true));
        AddLeave(1, _today, _today);
        AddLeave(3, _today, _today);

        var result = await CreateService().GetDashboard(Input());

        result.Scope.Reason.ShouldBe(DashboardScopeReason.Restricted);
        result.Scope.OrgUnitNames.ShouldBe(new[] { "Finance" });
        result.Scope.IncludesOwnTeam.ShouldBeTrue();
        result.Scope.EmployeesInScope.ShouldBe(2);
        result.OnLeaveToday.Select(r => r.EmployeeId).ShouldBe(new[] { 3 });

        // Employee 1's unit (Company) still appears, but only as Finance's parent in the tree.
        result.OrgUnitOptions.Select(o => o.Name).ShouldBe(new[] { "Company", "Finance", "Payroll" }, ignoreOrder: false);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_At_All_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));
        AddLeave(1, _today, _today);
        AddLeave(2, _today.AddDays(-9), _today.AddDays(-2));
        AddLeave(3, _today.AddDays(3), _today.AddDays(4), status: RequestStatus.Open);

        var result = await CreateService().GetDashboard(Input());

        result.Scope.Reason.ShouldBe(DashboardScopeReason.Nobody);
        result.Scope.EmployeesInScope.ShouldBe(0);
        result.OnLeaveToday.ShouldBeEmpty();
        result.NotReturned.ShouldBeEmpty();
        result.PendingApproval.ShouldBeEmpty();
        result.ByOrgUnit.ShouldBeEmpty();
        result.OrgUnitOptions.ShouldBeEmpty();
        result.Kpis.LeaveDaysInPeriod.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboard_Should_Not_Widen_A_Restricted_Scope_Through_The_Unit_Filter()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 3 }, new[] { "Payroll" }, includesOwnTeam: false));
        AddLeave(1, _today, _today);
        AddLeave(2, _today, _today);
        AddLeave(3, _today, _today);

        // Company is above Payroll; picking it must not bring in employees 1 and 2.
        var result = await CreateService().GetDashboard(Input(unitId: 1));

        result.OnLeaveToday.Select(r => r.EmployeeId).ShouldBe(new[] { 3 });
    }

    // ---------------------------------------------------------------- filters

    [Fact]
    public async Task GetDashboard_Should_Include_Units_Below_The_Selected_Unit()
    {
        AddLeave(1, _today, _today);
        AddLeave(2, _today, _today);
        AddLeave(3, _today, _today);

        var result = await CreateService().GetDashboard(Input(unitId: 2));

        result.OnLeaveToday.Select(r => r.EmployeeId).ShouldBe(new[] { 2, 3 }, ignoreOrder: true);
        result.OrgUnitPath.Select(p => p.Name).ShouldBe(new[] { "Company", "Finance" }, ignoreOrder: false);
        result.OrgUnitOptions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetDashboard_Should_Leave_Out_Hourly_Absences()
    {
        AddLeave(1, _today, _today, hours: 2);

        var result = await CreateService().GetDashboard(Input());

        result.Kpis.OnLeaveToday.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Open_Requests_As_Pending_Approval()
    {
        AddLeave(1, _today.AddDays(10), _today.AddDays(12), status: RequestStatus.Open);

        var result = await CreateService().GetDashboard(Input());

        result.Kpis.PendingApproval.ShouldBe(1);
        result.PendingApproval.Single().EmployeeCode.ShouldBe("E1");
    }

    [Fact]
    public async Task GetDashboard_Should_Default_To_The_Current_Year_And_Swap_A_Reversed_Range()
    {
        var service = CreateService();

        var defaulted = await service.GetDashboard(Input());
        defaulted.FromDate.ShouldBe(new DateTime(_today.Year, 1, 1));
        defaulted.ToDate.ShouldBe(new DateTime(_today.Year, 12, 31));

        var input = Input();
        input.FromDate = new DateTime(2026, 5, 31);
        input.ToDate = new DateTime(2026, 5, 1);
        var swapped = await service.GetDashboard(input);
        swapped.FromDate.ShouldBe(new DateTime(2026, 5, 1));
        swapped.ToDate.ShouldBe(new DateTime(2026, 5, 31));
        swapped.Granularity.ShouldBe(LeaveDashboardGranularity.Day);
    }

    [Fact]
    public async Task GetDashboard_Should_Cap_The_Period_At_Five_Years()
    {
        var input = Input();
        input.FromDate = new DateTime(1900, 1, 1);
        input.ToDate = new DateTime(2026, 12, 31);

        var result = await CreateService().GetDashboard(input);

        result.FromDate.ShouldBe(new DateTime(2022, 1, 1));
    }

    // ---------------------------------------------------------------- export

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        AddLeave(1, _today, _today);

        var file = await CreateService().ExportToExcel(new ExportLeaveDashboardInput { Part = LeaveDashboardExportPart.All });

        file.FileName.ShouldEndWith(".xlsx");
        using var book = OpenExport(file);
        book.Worksheets.Select(w => w.Name).ShouldBe(new[]
        {
            "Summary",
            "LeaveDashboard_OnLeaveToday",
            "LeaveDashboard_NotReturned",
            "LeaveDashboard_StartingSoon",
            "LeaveDashboard_ReturningSoon",
            "LeaveDashboard_PendingApproval",
            "LeaveDashboard_ByMonth",
            "LeaveDashboard_ByDepartment",
            "LeaveDashboard_ByType",
            "LeaveDashboard_LeaveInPeriod"
        }, ignoreOrder: false);

        // Row 3 is the header; the one leave is on row 4.
        book.Worksheet("LeaveDashboard_OnLeaveToday").Cell(4, 1).GetString().ShouldBe("E1");

        // The Summary says whose data it is, in the words the screen uses.
        book.Worksheet("Summary").Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
    }

    [Fact]
    public async Task ExportToExcel_Should_Name_The_Units_And_The_Team_On_The_Summary_Sheet()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2, 3 }, new[] { "Finance", "Payroll" }, includesOwnTeam: true));

        var file = await CreateService().ExportToExcel(new ExportLeaveDashboardInput { Part = LeaveDashboardExportPart.All });

        using var book = OpenExport(file);
        book.Worksheet("Summary").Cell(3, 2).GetString().ShouldBe("Finance + Payroll + Dashboard_ScopeYourTeam");
    }

    [Fact]
    public async Task ExportToExcel_Should_Export_Just_The_One_Tile_Asked_For()
    {
        AddLeave(2, _today.AddDays(-12), _today.AddDays(-5));

        var file = await CreateService().ExportToExcel(new ExportLeaveDashboardInput { Part = LeaveDashboardExportPart.NotReturned });

        using var book = OpenExport(file);
        var sheet = book.Worksheets.Single();
        sheet.Name.ShouldBe("LeaveDashboard_NotReturned");
        sheet.Cell(3, 8).GetString().ShouldBe("DaysOverdue");
        sheet.Cell(4, 1).GetString().ShouldBe("E2");
        sheet.Cell(4, 8).GetValue<int>().ShouldBe(5);
    }

    [Fact]
    public async Task ExportToExcel_Should_Export_Only_The_Leave_Covering_The_Clicked_Day()
    {
        AddLeave(1, _today.AddDays(-2), _today);
        AddLeave(2, _today.AddDays(1), _today.AddDays(2));

        var file = await CreateService().ExportToExcel(new ExportLeaveDashboardInput
        {
            FromDate = _today.AddDays(-30),
            ToDate = _today.AddDays(30),
            Part = LeaveDashboardExportPart.LeavesInPeriod,
            Day = _today
        });

        using var book = OpenExport(file);
        var sheet = book.Worksheets.Single();
        sheet.Cell(4, 1).GetString().ShouldBe("E1");
        sheet.Cell(5, 1).IsEmpty().ShouldBeTrue();
    }

    [Fact]
    public async Task ExportToExcel_Should_Apply_The_Same_Scope_As_The_Screen()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddLeave(1, _today, _today);
        AddLeave(2, _today, _today);

        var file = await CreateService().ExportToExcel(new ExportLeaveDashboardInput { Part = LeaveDashboardExportPart.OnLeaveToday });

        using var book = OpenExport(file);
        var sheet = book.Worksheets.Single();
        sheet.Cell(4, 1).GetString().ShouldBe("E2");
        sheet.Cell(5, 1).IsEmpty().ShouldBeTrue();
    }

    // ---------------------------------------------------------------- leave list

    [Fact]
    public async Task GetLeaves_Should_Return_Leave_Covering_The_Chosen_Day_Page_By_Page()
    {
        AddLeave(1, _today.AddDays(-2), _today);
        AddLeave(2, _today, _today.AddDays(3));
        AddLeave(3, _today.AddDays(1), _today.AddDays(2));

        var page = await CreateService().GetLeaves(new GetLeaveDashboardLeavesInput
        {
            FromDate = _today.AddDays(-30),
            ToDate = _today.AddDays(30),
            Day = _today,
            SkipCount = 1,
            MaxResultCount = 1
        });

        page.TotalCount.ShouldBe(2);
        page.Items.Single().EmployeeId.ShouldBe(1);
    }

    [Fact]
    public async Task GetLeaves_Should_Apply_The_Scope_Too()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 3 }, new List<string>(), includesOwnTeam: true));
        AddLeave(1, _today, _today);
        AddLeave(3, _today, _today);

        var page = await CreateService().GetLeaves(new GetLeaveDashboardLeavesInput { MaxResultCount = 10 });

        page.Items.Select(r => r.EmployeeId).ShouldBe(new[] { 3 });
    }
}
