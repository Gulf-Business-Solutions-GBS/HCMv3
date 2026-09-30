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
using HCMv3.Dashboards.Workforce;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

// See ProjectCostEngineFakes for why a plain List<T>.AsQueryable() is a complete stand-in here.
using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Workforce Dashboard shows whatever <see cref="IDashboardScopeService"/> resolves for the signed-in user,
/// narrowed by the unit and clicked-bar filters. Counting rules: <see cref="WorkforceDashboardCalculator_Tests"/>.
/// </summary>
public class WorkforceDashboardAppService_Tests
{
    private const int Kuwaiti = 1;

    private readonly DateTime _today = Clock.Now.Date;

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<EmployeeSeparation> _separations = new();
    private readonly List<Nationality> _nationalities = new() { new Nationality { Id = Kuwaiti, TenantId = 1, NameEn = "Kuwaiti" } };
    private readonly List<Qualification> _qualifications = new();
    private readonly List<JobTitle> _jobTitles = new();
    private readonly List<OrganizationUnit> _units = new()
    {
        new OrganizationUnit(1, "Company") { Id = 1 },
        new OrganizationUnit(1, "Finance", 1) { Id = 2 },
        new OrganizationUnit(1, "Payroll", 2) { Id = 3 }
    };

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    public WorkforceDashboardAppService_Tests()
    {
        AddEmployee(1, unitId: 1, gender: Gender.Male);
        AddEmployee(2, unitId: 2, gender: Gender.Female);
        AddEmployee(3, unitId: 3, gender: Gender.Female);

        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private XLWorkbook OpenExport(FileDto file) => new(new MemoryStream(_savedFiles[file.FileToken]));

    private void AddEmployee(int id, long unitId, Gender gender, bool active = true, DateTime? joined = null)
    {
        _employees.Add(new Employee
        {
            Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Gender = gender,
            BirthDate = new DateTime(1990, 1, 1), NationalityId = Kuwaiti, JoiningDate = joined ?? new DateTime(2020, 1, 1),
            Status = active ? EmployeeStatus.Active : EmployeeStatus.Inactive
        });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = new DateTime(2020, 1, 1), EndDate = DateTime.MaxValue.Date
        });
    }

    private void Separate(int employeeId, DateTime lastDay, DateTime? revoked = null)
    {
        _separations.Add(new EmployeeSeparation
        {
            Id = _separations.Count + 1, TenantId = 1, EmployeeId = employeeId, LastDayInService = lastDay,
            SeparationType = SeparationType.Resignation, RevokeDate = revoked
        });
        _employees.Single(e => e.Id == employeeId).Status = EmployeeStatus.Inactive;
    }

    private WorkforceDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var exporter = new WorkforceDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() };

        var service = new WorkforceDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_separations),
            Fakes.RepoOver(_nationalities),
            Fakes.RepoOver(_qualifications),
            Fakes.RepoOver(_jobTitles),
            unitRepository,
            _scopeService,
            exporter);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        return service;
    }

    // ---------------------------------------------------------------- scope

    [Fact]
    public async Task GetDashboard_Should_Count_Everyone_When_The_Scope_Is_Everyone()
    {
        var result = await CreateService().GetDashboard(new WorkforceDashboardInput());

        result.Scope.Reason.ShouldBe(DashboardScopeReason.Admin);
        result.Kpis.Headcount.ShouldBe(3);
        result.Kpis.Female.ShouldBe(2);
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Only_The_Employees_In_A_Restricted_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2, 3 }, new[] { "Finance" }, includesOwnTeam: false));

        var result = await CreateService().GetDashboard(new WorkforceDashboardInput());

        result.Kpis.Headcount.ShouldBe(2);
        result.Scope.EmployeesInScope.ShouldBe(2);
        result.OrgUnitOptions.Select(o => o.Name).ShouldBe(new[] { "Company", "Finance", "Payroll" }, ignoreOrder: false);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));

        var result = await CreateService().GetDashboard(new WorkforceDashboardInput());

        result.Scope.Reason.ShouldBe(DashboardScopeReason.Nobody);
        result.Kpis.Headcount.ShouldBe(0);
        result.Genders.ShouldBeEmpty();
        result.OrgUnitOptions.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- filters

    [Fact]
    public async Task GetDashboard_Should_Include_Units_Below_The_Selected_Unit()
    {
        var result = await CreateService().GetDashboard(new WorkforceDashboardInput { OrganizationUnitId = 2 });

        result.Kpis.Headcount.ShouldBe(2);
        result.OrgUnitPath.Select(p => p.Name).ShouldBe(new[] { "Company", "Finance" }, ignoreOrder: false);
    }

    [Fact]
    public async Task GetDashboard_Should_Apply_A_Clicked_Bar_To_Every_Tile()
    {
        var result = await CreateService().GetDashboard(new WorkforceDashboardInput { Gender = Gender.Male });

        result.Kpis.Headcount.ShouldBe(1);
        result.Genders.Single().Gender.ShouldBe(Gender.Male);
        result.ByOrgUnit.Single().Headcount.ShouldBe(1);
    }

    // ---------------------------------------------------------------- separations

    [Fact]
    public async Task GetDashboard_Should_Count_A_Separation_In_The_Period_As_A_Leaver()
    {
        var lastDay = _today.AddDays(-1);
        Separate(3, lastDay);

        var result = await CreateService().GetDashboard(new WorkforceDashboardInput
        {
            FromDate = lastDay.AddDays(-10),
            ToDate = _today
        });

        result.Kpis.Leavers.ShouldBe(1);
        result.Kpis.Resignations.ShouldBe(1);
        result.Kpis.Headcount.ShouldBe(2);
        result.Kpis.TurnoverPercent.ShouldBe(40); // 1 / ((3 + 2) / 2)
    }

    [Fact]
    public async Task GetDashboard_Should_Ignore_A_Revoked_Separation()
    {
        Separate(3, _today.AddDays(-1), revoked: _today.AddDays(-5));
        _employees.Single(e => e.Id == 3).Status = EmployeeStatus.Active;

        var result = await CreateService().GetDashboard(new WorkforceDashboardInput());

        result.Kpis.Leavers.ShouldBe(0);
        result.Kpis.Headcount.ShouldBe(3);
    }

    [Fact]
    public async Task GetDashboard_Should_Leave_Out_Inactive_Employees_With_No_Separation()
    {
        AddEmployee(4, unitId: 2, gender: Gender.Male, active: false);

        var result = await CreateService().GetDashboard(new WorkforceDashboardInput());

        result.Kpis.Headcount.ShouldBe(3);
        result.Kpis.Leavers.ShouldBe(0);
    }

    // ---------------------------------------------------------------- lists and export

    [Fact]
    public async Task GetEmployees_Should_List_The_Joiners_Page_By_Page()
    {
        AddEmployee(4, unitId: 2, gender: Gender.Male, joined: _today.AddDays(-3));
        AddEmployee(5, unitId: 2, gender: Gender.Male, joined: _today.AddDays(-2));

        var page = await CreateService().GetEmployees(new GetWorkforceEmployeesInput
        {
            FromDate = _today.AddDays(-30),
            ToDate = _today,
            Kind = WorkforceEmployeeListKind.Joiners,
            SkipCount = 1,
            MaxResultCount = 1
        });

        page.TotalCount.ShouldBe(2);
        page.Items.Single().EmployeeCode.ShouldBe("E5");
    }

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        var file = await CreateService().ExportToExcel(new ExportWorkforceDashboardInput { Part = WorkforceDashboardExportPart.All });

        using var book = OpenExport(file);
        book.Worksheets.Select(w => w.Name).ShouldBe(new[]
        {
            "Summary",
            "WorkforceDashboard_ByMonth",
            "WorkforceDashboard_ByAge",
            "WorkforceDashboard_ByGender",
            "WorkforceDashboard_ByService",
            "WorkforceDashboard_ByNationalit", // the echoed key, cut to Excel's 31-character limit
            "WorkforceDashboard_ByQualificat",
            "WorkforceDashboard_ByJobTitle",
            "WorkforceDashboard_ByDepartment",
            "WorkforceDashboard_Headcount",
            "WorkforceDashboard_Joiners",
            "WorkforceDashboard_Leavers"
        }, ignoreOrder: false);

        book.Worksheet("Summary").Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
        book.Worksheet("WorkforceDashboard_Headcount").Cell(4, 1).GetString().ShouldBe("E1");
    }

    [Fact]
    public async Task ExportToExcel_Should_Export_Just_The_One_Tile_Asked_For_With_The_Same_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));

        var file = await CreateService().ExportToExcel(new ExportWorkforceDashboardInput { Part = WorkforceDashboardExportPart.Headcount });

        using var book = OpenExport(file);
        var sheet = book.Worksheets.Single();
        sheet.Cell(4, 1).GetString().ShouldBe("E2");
        sheet.Cell(5, 1).IsEmpty().ShouldBeTrue();
    }
}
