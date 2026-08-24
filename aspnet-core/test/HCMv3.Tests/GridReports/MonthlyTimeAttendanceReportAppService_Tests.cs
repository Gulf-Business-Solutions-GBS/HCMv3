using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Monthly Time Attendance grid report (legacy <c>uscRptMonthlyTimeAttendance.ascx</c>,
/// <c>Table1()</c>): a headcount per department of the employees who have ANY late/early figure in the
/// window.
///
/// Two selection rules carry the whole report and are pinned separately here: the "at least one non-zero
/// late/early figure" gate (legacy lines 486-490) and the "must have a department" gate (legacy line
/// 490's <c>CurrentDepartment != null</c>), which DROPS employees rather than bucketing them into "".
/// </summary>
// ReSharper disable once InconsistentNaming
public class MonthlyTimeAttendanceReportAppService_Tests : AppTestBase
{
    private readonly IMonthlyTimeAttendanceReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public MonthlyTimeAttendanceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IMonthlyTimeAttendanceReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    /// <summary>
    /// Builds an org unit that actually RESOLVES to a department name: the name only appears once the
    /// unit carries an OrgUnitProfile classified at one of the seven Level codes, which is what the
    /// report reads in place of legacy's <c>Employee.CurrentDepartment</c>.
    /// </summary>
    private long CreateClassifiedOrganizationUnit(string displayName)
    {
        long id = 0;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(
                new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
                {
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8)
                }).Entity;

            var unitType = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = TenantId,
                Code = OrganizationUnitType.Level1Code,
                NameEn = "Level 1",
                NameAr = "Level 1"
            }).Entity;

            context.SaveChanges();

            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId,
                OrganizationUnitId = unit.Id,
                Code = unit.Code,
                NameAr = displayName + "-Ar",
                OrganizationUnitTypeId = unitType.Id
            });

            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    /// <summary>The department is read from the assignment covering TODAY, not the report window.</summary>
    private void AssignToday(Employee employee, long? organizationUnitId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = DateTime.Now.Date.AddDays(-1),
                EndDate = DateTime.Now.Date.AddDays(1),
                OrganizationUnitId = organizationUnitId
            });
        });
    }

    private void CreateTimeRegister(
        Employee employee,
        DateTime dayDate,
        double lateIn = 0,
        double deductionLateIn = 0,
        double earlyOut = 0,
        double deductionEarlyOut = 0,
        double dedAfterMonthlyTol = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = dayDate,
                LateIn = lateIn,
                DeductionLateIn = deductionLateIn,
                EarlyOut = earlyOut,
                DeductionEarlyOut = deductionEarlyOut,
                DedAfterMonthlyTol = dedAfterMonthlyTol
            });
        });
    }

    private static GetMonthlyTimeAttendanceReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetMonthlyTimeAttendanceReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    /// <summary>
    /// Legacy lines 486-490: the register row counts only when at least ONE of LateIn, DeductionLateIn,
    /// EarlyOut, DeductionEarlyOut or DedAfterMonthlyTol is greater than zero.
    /// </summary>
    [Fact]
    public async Task Should_Only_Count_Employees_With_A_Non_Zero_Late_Or_Early_Figure()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Gate");

        var late = CreateEmployee("MTA-LATE");
        var clean = CreateEmployee("MTA-CLEAN");
        AssignToday(late, unitId);
        AssignToday(clean, unitId);

        var day = new DateTime(2024, 6, 10);
        CreateTimeRegister(late, day, lateIn: 15);
        CreateTimeRegister(clean, day);

        var lateOnly = await _appService.GetMonthlyTimeAttendanceReport(Input(day, day, employeeCode: "MTA-LATE"));
        var cleanOnly = await _appService.GetMonthlyTimeAttendanceReport(Input(day, day, employeeCode: "MTA-CLEAN"));

        lateOnly.Items.Single().NoOfEmployees.ShouldBe(1);
        cleanOnly.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// Any ONE of the five figures is enough -- this pins the OR, not just the LateIn leg.
    /// </summary>
    [Fact]
    public async Task Should_Count_An_Employee_Whose_Only_Non_Zero_Figure_Is_The_Monthly_Tolerance_Deduction()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Tol");

        var employee = CreateEmployee("MTA-TOL");
        AssignToday(employee, unitId);

        var day = new DateTime(2024, 6, 11);
        CreateTimeRegister(employee, day, dedAfterMonthlyTol: 5);

        var result = await _appService.GetMonthlyTimeAttendanceReport(Input(day, day, employeeCode: "MTA-TOL"));

        result.Items.Single().NoOfEmployees.ShouldBe(1);
    }

    /// <summary>
    /// Legacy line 490 required <c>Employee.CurrentDepartment != null</c>: an employee with no department
    /// is DROPPED from the report entirely, not shown under a blank department name.
    /// </summary>
    [Fact]
    public async Task Should_Drop_Employees_That_Have_No_Department()
    {
        var noAssignment = CreateEmployee("MTA-NODEPT");

        var day = new DateTime(2024, 6, 12);
        CreateTimeRegister(noAssignment, day, lateIn: 20);

        var result = await _appService.GetMonthlyTimeAttendanceReport(Input(day, day, employeeCode: "MTA-NODEPT"));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Include_The_Days_On_Both_Window_Boundaries_And_Exclude_The_Neighbours()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Edge");

        var onFromDate = CreateEmployee("MTA-EDGE-FROM");
        var onToDate = CreateEmployee("MTA-EDGE-TO");
        var justOutside = CreateEmployee("MTA-EDGE-OUT");
        AssignToday(onFromDate, unitId);
        AssignToday(onToDate, unitId);
        AssignToday(justOutside, unitId);

        // Legacy predicate is DayDate >= dtFrom && DayDate <= dtTo -- both ends inclusive.
        CreateTimeRegister(onFromDate, new DateTime(2024, 6, 1), lateIn: 5);
        CreateTimeRegister(onToDate, new DateTime(2024, 6, 30), lateIn: 5);
        CreateTimeRegister(justOutside, new DateTime(2024, 7, 1), lateIn: 5);

        var result = await _appService.GetMonthlyTimeAttendanceReport(
            Input(
                new DateTime(2024, 6, 1),
                new DateTime(2024, 6, 30),
                employeeCode: "MTA-EDGE-FROM,MTA-EDGE-TO,MTA-EDGE-OUT"));

        // The two in-window employees share one department; the out-of-window one contributes nothing.
        result.Items.Single().NoOfEmployees.ShouldBe(2);
    }

    /// <summary>
    /// The measure is a count of DISTINCT employees, so several register rows for one employee must not
    /// inflate it.
    /// </summary>
    [Fact]
    public async Task Should_Count_Each_Employee_Once_However_Many_Register_Rows_They_Have()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Distinct");

        var employee = CreateEmployee("MTA-DISTINCT");
        AssignToday(employee, unitId);

        CreateTimeRegister(employee, new DateTime(2024, 6, 1), lateIn: 5);
        CreateTimeRegister(employee, new DateTime(2024, 6, 2), lateIn: 5);
        CreateTimeRegister(employee, new DateTime(2024, 6, 3), earlyOut: 5);

        var result = await _appService.GetMonthlyTimeAttendanceReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "MTA-DISTINCT"));

        result.Items.Single().NoOfEmployees.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Status");

        var active = CreateEmployee("MTA-ST-ACTIVE");
        var inactive = CreateEmployee("MTA-ST-INACTIVE", EmployeeStatus.Inactive);
        AssignToday(active, unitId);
        AssignToday(inactive, unitId);

        var day = new DateTime(2024, 6, 13);
        CreateTimeRegister(active, day, lateIn: 5);
        CreateTimeRegister(inactive, day, lateIn: 5);

        var result = await _appService.GetMonthlyTimeAttendanceReport(
            Input(day, day, employeeCode: "MTA-ST-ACTIVE", status: EmployeeStatus.Inactive));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Export");

        var employee = CreateEmployee("MTA-EXP");
        AssignToday(employee, unitId);
        CreateTimeRegister(employee, new DateTime(2024, 6, 14), lateIn: 5);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetMonthlyTimeAttendanceReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "MTA-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("MonthlyTimeAttendance.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("Department");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var unitId = CreateClassifiedOrganizationUnit("MTA Dept Header");

        var employee = CreateEmployee("MTA-HDR");
        AssignToday(employee, unitId);
        CreateTimeRegister(employee, new DateTime(2024, 6, 15), lateIn: 5);

        var file = await _appService.GetMonthlyTimeAttendanceReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "MTA-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
