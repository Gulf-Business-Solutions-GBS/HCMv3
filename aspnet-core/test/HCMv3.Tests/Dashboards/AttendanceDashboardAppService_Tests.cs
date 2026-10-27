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
using HCMv3.Dashboards.Attendance;
using HCMv3.Dashboards.Attendance.Dto;
using HCMv3.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using NSubstitute;
using Shouldly;
using Xunit;

using Fakes = HCMv3.Tests.ProjectManagement.ProjectCostEngineFakes;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// The Time &amp; Attendance Dashboard reads the Time Engine's registers for the employees
/// <see cref="IDashboardScopeService"/> allows, narrowed by unit and employee. Rules:
/// <see cref="AttendanceDashboardCalculator_Tests"/>. Period used: 1-31 March 2026.
/// </summary>
public class AttendanceDashboardAppService_Tests
{
    private static readonly DateTime March1 = new(2026, 3, 1);

    private readonly List<Employee> _employees = new();
    private readonly List<EmployeeOrgAssignment> _assignments = new();
    private readonly List<TimeRegister> _registers = new();
    private readonly List<TimeEvent> _events = new();
    private readonly List<TransType> _transTypes = new() { new TransType { Id = 1, TenantId = 1, Code = "1", NameEn = "Clock In" } };
    private readonly List<OrganizationUnit> _units = new()
    {
        new OrganizationUnit(1, "Company") { Id = 1 },
        new OrganizationUnit(1, "Finance", 1) { Id = 2 }
    };

    private readonly IDashboardScopeService _scopeService = Substitute.For<IDashboardScopeService>();
    private readonly ISysSettingsAccessor _settings = Substitute.For<ISysSettingsAccessor>();
    private readonly ITempFileCacheManager _tempFiles = Substitute.For<ITempFileCacheManager>();
    private readonly Dictionary<string, byte[]> _savedFiles = new();

    public AttendanceDashboardAppService_Tests()
    {
        AddEmployee(1, unitId: 1);
        AddEmployee(2, unitId: 2);

        ScopeIs(DashboardScope.Everyone(DashboardScopeReason.Admin));
        ApprovalOn(false);

        _tempFiles.When(t => t.SetFile(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(call => _savedFiles[call.ArgAt<string>(0)] = call.ArgAt<byte[]>(1));
    }

    private void ScopeIs(DashboardScope scope) => _scopeService.GetScopeAsync().Returns(Task.FromResult(scope));

    private void ApprovalOn(bool on) =>
        _settings.GetTimeSettingsAsync().Returns(Task.FromResult(new ManageTimeSettings { EnableTimeRegisterApproval = on }));

    private void AddEmployee(int id, long unitId)
    {
        _employees.Add(new Employee { Id = id, TenantId = 1, Code = "E" + id, FullNameEn = "Employee " + id, Status = EmployeeStatus.Active });
        _assignments.Add(new EmployeeOrgAssignment
        {
            Id = id, TenantId = 1, EmployeeId = id, OrganizationUnitId = unitId,
            StartDate = new DateTime(2020, 1, 1), EndDate = DateTime.MaxValue.Date
        });
    }

    private void AddDay(int employeeId, int day, double absent = 0, double approvedAbsent = 0, double late = 0)
    {
        var date = March1.AddDays(day - 1);
        _registers.Add(new TimeRegister
        {
            Id = _registers.Count + 1, TenantId = 1, EmployeeId = employeeId, DayDate = date,
            PlannedWorkingDay = 1, AbsentDay = absent, ApprovedAbsentDay = approvedAbsent, LateIn = late,
            ActualIn = absent > 0 ? default : date.AddHours(8), ActualOut = absent > 0 ? default : date.AddHours(16),
            PlannedWorkingMins = 480, ActualWorkingMins = absent > 0 ? 0 : 480
        });
    }

    private AttendanceDashboardAppService CreateService()
    {
        var unitRepository = Substitute.For<IRepository<OrganizationUnit, long>>();
        unitRepository.GetAll().Returns(_ => _units.AsQueryable());

        var exporter = new AttendanceDashboardExcelExporter(_tempFiles) { LocalizationManager = Fakes.LocalizationManager() };

        var service = new AttendanceDashboardAppService(
            Fakes.RepoOver(_employees),
            Fakes.RepoOver(_assignments),
            Fakes.RepoOver(_registers),
            Fakes.RepoOver(_events),
            Fakes.RepoOver(_transTypes),
            unitRepository,
            _settings,
            _scopeService,
            exporter);

        ((ApplicationService)service).AbpSession = Fakes.Session();
        ((ApplicationService)service).LocalizationManager = Fakes.LocalizationManager();
        ((ApplicationService)service).UnitOfWorkManager = Fakes.UnitOfWorkManager();
        return service;
    }

    private static AttendanceDashboardInput March(long? unitId = null, int? employeeId = null) =>
        new() { FromDate = March1, ToDate = new DateTime(2026, 3, 31), OrganizationUnitId = unitId, EmployeeId = employeeId };

    [Fact]
    public async Task GetDashboard_Should_Count_The_Registers_Of_The_Period()
    {
        AddDay(1, 2);
        AddDay(1, 3, absent: 1);
        AddDay(2, 2, late: 30);
        AddDay(2, 3);

        var result = await CreateService().GetDashboard(March());

        result.Kpis.WorkingDays.ShouldBe(4);
        result.Kpis.AbsentDays.ShouldBe(1);
        result.Kpis.AttendanceRatePercent.ShouldBe(75);
        result.Kpis.LateDays.ShouldBe(1);
        result.Granularity.ShouldBe(AttendanceDashboardGranularity.Day);
    }

    /// <summary>Legacy and the payroll engine both read the approved absence when time-register approval is on.</summary>
    [Fact]
    public async Task GetDashboard_Should_Use_Approved_Absence_When_Register_Approval_Is_On()
    {
        AddDay(1, 2, absent: 1, approvedAbsent: 0);
        AddDay(1, 3, absent: 0, approvedAbsent: 1);
        ApprovalOn(true);

        var result = await CreateService().GetDashboard(March());

        result.Kpis.AbsentDays.ShouldBe(1);
        (await CreateService().GetExceptions(new GetAttendanceExceptionsInput
        {
            FromDate = March1, ToDate = new DateTime(2026, 3, 31), Kind = AttendanceExceptionKind.Absent, MaxResultCount = 10
        })).Items.Single().DayDate.Day.ShouldBe(3);
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Only_The_Employees_In_Scope()
    {
        ScopeIs(DashboardScope.Restricted(new[] { 2 }, new List<string>(), includesOwnTeam: true));
        AddDay(1, 2, absent: 1);
        AddDay(2, 2);

        var result = await CreateService().GetDashboard(March());

        result.Kpis.WorkingDays.ShouldBe(1);
        result.Kpis.AbsentDays.ShouldBe(0);
        result.Scope.EmployeesInScope.ShouldBe(1);
    }

    [Fact]
    public async Task GetDashboard_Should_Show_Nothing_When_The_Scope_Is_Nobody()
    {
        ScopeIs(DashboardScope.Restricted(new List<int>(), new List<string>(), includesOwnTeam: false));
        AddDay(1, 2, absent: 1);

        var result = await CreateService().GetDashboard(March());

        result.Kpis.WorkingDays.ShouldBe(0);
        result.TopAbsent.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_Should_Narrow_To_A_Unit_And_To_One_Clicked_Employee()
    {
        AddDay(1, 2, absent: 1);
        AddDay(2, 2);

        (await CreateService().GetDashboard(March(unitId: 2))).Kpis.WorkingDays.ShouldBe(1);

        var one = await CreateService().GetDashboard(March(employeeId: 1));
        one.Kpis.AbsentDays.ShouldBe(1);
        one.EmployeeNameEn.ShouldBe("Employee 1");
    }

    [Fact]
    public async Task GetDashboard_Should_Count_Pending_Punch_Requests()
    {
        _events.Add(new TimeEvent
        {
            Id = 1, TenantId = 1, EmployeeId = 1, TransTypeId = 1, TransDate = March1.AddDays(4),
            TransTime = March1.AddDays(4).AddHours(8), WorkflowRequestId = 7, WorkflowStatus = RequestStatus.Open
        });
        _events.Add(new TimeEvent
        {
            Id = 2, TenantId = 1, EmployeeId = 1, TransTypeId = 1, TransDate = March1.AddDays(4),
            TransTime = March1.AddDays(4).AddHours(9) // an ordinary device punch, not a request
        });

        var result = await CreateService().GetDashboard(March());

        result.Kpis.PunchRequests.ShouldBe(1);
        result.Kpis.PunchRequestsPending.ShouldBe(1);
        (await CreateService().GetPunchRequests(new GetAttendancePunchRequestsInput
        {
            FromDate = March1, ToDate = new DateTime(2026, 3, 31), MaxResultCount = 10
        })).Items.Single().PunchTypeNameEn.ShouldBe("Clock In");
    }

    [Fact]
    public async Task GetExceptions_Should_List_A_Clicked_Day()
    {
        AddDay(1, 2, absent: 1);
        AddDay(2, 2, late: 10);
        AddDay(2, 3, late: 10);

        var page = await CreateService().GetExceptions(new GetAttendanceExceptionsInput
        {
            FromDate = March1, ToDate = new DateTime(2026, 3, 31), Kind = AttendanceExceptionKind.Any,
            Day = March1.AddDays(1), MaxResultCount = 10
        });

        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task ExportToExcel_Should_Put_The_Whole_Dashboard_On_A_Summary_Sheet_Plus_One_Sheet_Per_Tile()
    {
        AddDay(1, 2, absent: 1);

        var file = await CreateService().ExportToExcel(new ExportAttendanceDashboardInput
        {
            FromDate = March1, ToDate = new DateTime(2026, 3, 31), Part = AttendanceDashboardExportPart.All
        });

        using var book = new XLWorkbook(new MemoryStream(_savedFiles[file.FileToken]));
        book.Worksheets.Count.ShouldBe(9);
        book.Worksheet(1).Name.ShouldBe("Summary");
        book.Worksheet(1).Cell(3, 2).GetString().ShouldBe("Dashboard_ScopeAllAdmin");
    }
}
