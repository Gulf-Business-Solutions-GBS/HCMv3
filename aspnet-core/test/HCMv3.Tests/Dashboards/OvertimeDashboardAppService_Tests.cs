using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Organizations;
using ClosedXML.Excel;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Overtime;
using HCMv3.Dashboards.Overtime.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.Workflow;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Overtime Dashboard: entries and payroll cost for the employees <see cref="IDashboardScopeService"/> allows,
/// narrowed by unit, type and employee. Rules: <see cref="OvertimeDashboardCalculator_Tests"/>. Period: Q1 2026.
/// The list-backed fakes do not apply the ApprovedOnly filter, so only the pending test uses a workflow status.
/// </summary>
public class OvertimeDashboardAppService_Tests
{
    private const int Normal = 1;
    private const int Weekend = 2;
    private const int NormalOtWage = 1007;
    private const int WeekendOtWage = 1008;

    private static readonly DateTime Jan = new(2026, 1, 31);
    private static readonly DateTime Feb = new(2026, 2, 28);

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<EmployeeOvertime> _entries = new();
    private readonly List<OvertimeType> _types = new()
    {
        new OvertimeType { Id = Normal, TenantId = 1, Code = "1", NameEn = "Normal Day OT", WageTypeId = NormalOtWage },
        new OvertimeType { Id = Weekend, TenantId = 1, Code = "2", NameEn = "Weekend OT", WageTypeId = WeekendOtWage }
    };
    private readonly List<SalaryRegister> _registers = new();
    private readonly List<SalaryRegisterDetail> _details = new();
    private readonly List<OrganizationUnit> _units = new()
    {
        new OrganizationUnit(1, "Company") { Id = 1 },
        new OrganizationUnit(1, "Finance", 1) { Id = 2 }
    };

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    public OvertimeDashboardAppService_Tests()
    {
        AddEmployee(1, unitId: 1);
        AddEmployee(2, unitId: 2);
        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private void AddEmployee(int id, long unitId)
    {
        _employees.Add(new Employee { Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Status = EmployeeStatus.Active });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = new DateTime(2020, 1, 1), EndDate = DateTime.MaxValue.Date
        });
    }

    private void AddEntry(int employeeId, DateTime month, int type, double hours, RequestStatus? status = null) =>
        _entries.Add(new EmployeeOvertime
        {
            Id = _entries.Count + 1, TenantId = 1, EmployeeId = employeeId, EffectiveDate = month, OvertimeTypeId = type,
            OvertimeHours = hours, ActualOvertimeHours = hours, ApprovedOvertimeHours = status == null ? hours : 0, WorkflowStatus = status
        });

    /// <summary>A payroll result; <paramref name="inPeriod"/> later than the month makes it a retro recalculation.</summary>
    private void AddPay(int employeeId, DateTime month, int wageTypeId, double amount, DateTime? inPeriod = null)
    {
        var register = new SalaryRegister
        {
            Id = _registers.Count + 1, TenantId = 1, EmployeeId = employeeId, ForPeriod = month, InPeriod = inPeriod ?? month
        };
        _registers.Add(register);
        _details.Add(new SalaryRegisterDetail
        {
            Id = _details.Count + 1, TenantId = 1, SalaryRegisterId = register.Id, WageTypeId = wageTypeId, Amount = amount
        });
    }

    private OvertimeDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var service = new OvertimeDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_entries),
            Fakes.RepoOver(_types),
            Fakes.RepoOver(_registers),
            Fakes.RepoOver(_details),
            unitRepository,
            _scopeService,
            new OvertimeDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() });

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        ((ApplicationService)service).UnitOfWorkManager = Fakes.UnitOfWorkManager();
        return service;
    }

    private static OvertimeDashboardInput Q1(long? unitId = null, int? typeId = null, int? employeeId = null) => new()
    {
        FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31),
        OrganizationUnitId = unitId, OvertimeTypeId = typeId, EmployeeId = employeeId
    };

    [Fact]
    public async Task GetDashboard_Should_Sum_Hours_And_What_Payroll_Paid()
    {
        AddEntry(1, Jan, Normal, 8);
        AddEntry(2, Feb, Weekend, 4);
        AddPay(1, Jan, NormalOtWage, 16);
        AddPay(2, Feb, WeekendOtWage, 10);
        AddPay(2, Feb, 1000, 500); // basic pay: not overtime

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.ApprovedHours.ShouldBe(12);
        result.Kpis.Employees.ShouldBe(2);
        result.Kpis.Cost.ShouldBe(26);
        result.Types.Count.ShouldBe(2);
        result.Trend.Count.ShouldBe(3);
    }

    /// <summary>A retro recalculation carries the month again; counting it too would double the cost.</summary>
    [Fact]
    public async Task GetDashboard_Should_Count_Only_Each_Months_Regular_Payroll_Result()
    {
        AddPay(1, Jan, NormalOtWage, 16);
        AddPay(1, Jan, NormalOtWage, 18, inPeriod: Feb);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.Cost.ShouldBe(16);
    }

    [Fact]
    public async Task GetDashboard_Should_Narrow_By_Type_Unit_And_Employee()
    {
        AddEntry(1, Jan, Normal, 8);
        AddEntry(2, Jan, Weekend, 4);
        AddPay(1, Jan, NormalOtWage, 16);
        AddPay(2, Jan, WeekendOtWage, 10);

        var byType = await CreateService().GetDashboard(Q1(typeId: Weekend));
        byType.Kpis.ApprovedHours.ShouldBe(4);
        byType.Kpis.Cost.ShouldBe(10);

        (await CreateService().GetDashboard(Q1(unitId: 2))).Kpis.ApprovedHours.ShouldBe(4);

        var one = await CreateService().GetDashboard(Q1(employeeId: 1));
        one.Kpis.ApprovedHours.ShouldBe(8);
        one.EmployeeNameEn.ShouldBe("Employee 1");
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Only_The_Employees_In_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddEntry(1, Jan, Normal, 8);
        AddEntry(2, Jan, Weekend, 4);
        AddPay(1, Jan, NormalOtWage, 16);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.ApprovedHours.ShouldBe(4);
        result.Kpis.Cost.ShouldBe(0);
        result.Scope.EmployeesInScope.ShouldBe(1);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));
        AddEntry(1, Jan, Normal, 8);

        var result = await CreateService().GetDashboard(Q1());

        result.Kpis.ApprovedHours.ShouldBe(0);
        result.TopEmployees.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetEntries_Should_List_Pending_Requests_And_A_Clicked_Month()
    {
        AddEntry(1, Jan, Normal, 8);
        AddEntry(2, Feb, Normal, 3);
        AddEntry(2, Feb, Weekend, 6, RequestStatus.Open);

        var pending = await CreateService().GetEntries(new GetOvertimeEntriesInput
        {
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), Kind = OvertimeEntryListKind.Pending, MaxResultCount = 10
        });
        pending.Items.Single().ClaimedHours.ShouldBe(6);

        var january = await CreateService().GetEntries(new GetOvertimeEntriesInput
        {
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), Kind = OvertimeEntryListKind.Approved,
            Month = new DateTime(2026, 1, 15), MaxResultCount = 10
        });
        january.Items.Single().EmployeeCode.ShouldBe("E1");
    }

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        AddEntry(1, Jan, Normal, 8);

        var file = await CreateService().ExportToExcel(new ExportOvertimeDashboardInput
        {
            FromDate = new DateTime(2026, 1, 1), ToDate = new DateTime(2026, 3, 31), Part = OvertimeDashboardExportPart.All
        });

        using var book = new XLWorkbook(new MemoryStream(_savedFiles[file.FileToken]));
        book.Worksheets.Count.ShouldBe(6);
        book.Worksheet(1).Name.ShouldBe("Summary");
        book.Worksheet(1).Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
    }
}
