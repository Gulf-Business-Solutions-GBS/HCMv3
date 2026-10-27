using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Attendance Log grid report (legacy <c>uscRptEmpAttendanceLog.ascx</c>,
/// <c>Table1()</c>) -- a PUNCH-PAIRING STATE MACHINE rather than a query, so most of these tests pin the
/// emergent behaviour of that loop, including the punch it drops.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmpAttendanceLogReportAppService_Tests : AppTestBase
{
    private readonly IEmpAttendanceLogReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly int _clockInTransTypeId;
    private readonly int _clockOutTransTypeId;

    public EmpAttendanceLogReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmpAttendanceLogReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _clockInTransTypeId = CreateTransType("1");
        _clockOutTransTypeId = CreateTransType("2");
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateTransType(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "T" + code,
                NameAr = "T" + code
            }).Entity;
            context.SaveChanges();
            id = transType.Id;
        });

        return id;
    }

    private long CreateOrganizationUnit(string displayName)
    {
        long id = 0;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(
                new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
                {
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8)
                }).Entity;
            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    /// <summary>
    /// Creates an Active employee that is "in the org tree today" -- legacy's
    /// <c>Status == Active &amp;&amp; CurrentDepartment != null</c> scope.
    /// </summary>
    private Employee CreateEmployee(string code, bool assignToOrgUnit = true, EmployeeStatus status = EmployeeStatus.Active)
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
            context.SaveChanges();
        });

        if (assignToOrgUnit)
        {
            var unitId = CreateOrganizationUnit("OU-" + code);
            UsingDbContext(context =>
            {
                context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
                {
                    TenantId = TenantId,
                    EmployeeId = employee.Id,
                    StartDate = Clock.Now.Date.AddYears(-1),
                    EndDate = Clock.Now.Date.AddYears(1),
                    OrganizationUnitId = unitId
                });
            });
        }

        return employee;
    }

    /// <summary>Adds one punch. <paramref name="isIn"/> picks TransType "1" (in) or "2" (out).</summary>
    private void CreatePunch(Employee employee, DateTime day, int hour, int minute, bool isIn)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransTypeId = isIn ? _clockInTransTypeId : _clockOutTransTypeId,
                TransDate = day,
                TransTime = day.AddHours(hour).AddMinutes(minute)
            });
        });
    }

    private static GetEmpAttendanceLogReportInput Input(DateTime from, DateTime to, int maxResultCount = 100)
    {
        return new GetEmpAttendanceLogReportInput
        {
            FromDate = from,
            ToDate = to,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Pair_Punches_And_Total_The_Day_On_The_Last_Row()
    {
        var employee = CreateEmployee("EAL-PAIR");
        var day = new DateTime(2024, 6, 10);

        CreatePunch(employee, day, 8, 0, isIn: true);
        CreatePunch(employee, day, 12, 0, isIn: false);
        CreatePunch(employee, day, 13, 0, isIn: true);
        CreatePunch(employee, day, 17, 30, isIn: false);

        var result = await _appService.GetEmpAttendanceLogReport(Input(day, day));

        var rows = result.Items.Where(r => r.EmployeeCode == "EAL-PAIR").OrderBy(r => r.SignIn).ToList();
        rows.Count.ShouldBe(2);

        rows[0].SignIn.ShouldBe("08:00");
        rows[0].SignOut.ShouldBe("12:00");
        // Legacy renders each pair UNPADDED.
        rows[0].WorkingHours.ShouldBe("4:0:0");
        // Only the row closed by the day's last punch carries the total.
        rows[0].TotalWorkingHours.ShouldBe(string.Empty);

        rows[1].SignIn.ShouldBe("13:00");
        rows[1].SignOut.ShouldBe("17:30");
        rows[1].WorkingHours.ShouldBe("4:30:0");
        rows[1].TotalWorkingHours.ShouldBe("08:30:00");
    }

    /// <summary>
    /// LEGACY DEFECT PIN. A day whose LAST punch is an IN loses that punch: the row it opened is never
    /// appended, because the append only happens on the following IN or on the day's final OUT
    /// (uscRptEmpAttendanceLog.ascx.cs:237/291-297).
    /// </summary>
    [Fact]
    public async Task Should_Drop_A_Trailing_Unclosed_Punch_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("EAL-DROP");
        var day = new DateTime(2024, 6, 11);

        CreatePunch(employee, day, 8, 0, isIn: true);
        CreatePunch(employee, day, 12, 0, isIn: false);
        // Punched back in and never out again -- legacy emits NO row for this one.
        CreatePunch(employee, day, 13, 0, isIn: true);

        var result = await _appService.GetEmpAttendanceLogReport(Input(day, day));

        var rows = result.Items.Where(r => r.EmployeeCode == "EAL-DROP").ToList();
        rows.Count.ShouldBe(1);
        rows.Single().SignIn.ShouldBe("08:00");
        rows.Single().SignOut.ShouldBe("12:00");
    }

    [Fact]
    public async Task Should_Include_Punches_On_Both_Window_Boundaries_And_Exclude_The_Neighbours()
    {
        var employee = CreateEmployee("EAL-EDGE");

        foreach (var day in new[]
                 {
                     new DateTime(2024, 5, 31),
                     new DateTime(2024, 6, 1),
                     new DateTime(2024, 6, 30),
                     new DateTime(2024, 7, 1)
                 })
        {
            CreatePunch(employee, day, 8, 0, isIn: true);
            CreatePunch(employee, day, 16, 0, isIn: false);
        }

        var result = await _appService.GetEmpAttendanceLogReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30)));

        var days = result.Items
            .Where(r => r.EmployeeCode == "EAL-EDGE")
            .Select(r => r.DayDate)
            .OrderBy(d => d)
            .ToList();

        days.ShouldBe(new List<DateTime> { new(2024, 6, 1), new(2024, 6, 30) });
    }

    [Fact]
    public async Task Should_Exclude_Employees_That_Are_Not_In_The_Org_Tree_Today()
    {
        var inTree = CreateEmployee("EAL-IN-TREE");
        var notInTree = CreateEmployee("EAL-NO-TREE", assignToOrgUnit: false);
        var day = new DateTime(2024, 6, 12);

        foreach (var employee in new[] { inTree, notInTree })
        {
            CreatePunch(employee, day, 8, 0, isIn: true);
            CreatePunch(employee, day, 16, 0, isIn: false);
        }

        var result = await _appService.GetEmpAttendanceLogReport(Input(day, day));

        var codes = result.Items.Select(r => r.EmployeeCode).ToList();
        codes.ShouldContain("EAL-IN-TREE");
        codes.ShouldNotContain("EAL-NO-TREE");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("EAL-EXP");

        foreach (var day in new[]
                 {
                     new DateTime(2024, 6, 1),
                     new DateTime(2024, 6, 2),
                     new DateTime(2024, 6, 3)
                 })
        {
            CreatePunch(employee, day, 8, 0, isIn: true);
            CreatePunch(employee, day, 16, 0, isIn: false);
        }

        // Outside the window -- must not reach the export.
        CreatePunch(employee, new DateTime(2024, 7, 1), 8, 0, isIn: true);
        CreatePunch(employee, new DateTime(2024, 7, 1), 16, 0, isIn: false);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmpAttendanceLogReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), maxResultCount: 1));

        file.FileName.ShouldBe("EmpAttendanceLog.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count(r => r["Code"]?.ToString() == "EAL-EXP").ShouldBe(3);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("EAL-HDR");
        var day = new DateTime(2024, 6, 5);
        CreatePunch(employee, day, 8, 0, isIn: true);
        CreatePunch(employee, day, 16, 0, isIn: false);

        var file = await _appService.GetEmpAttendanceLogReportToExcel(Input(day, day));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
