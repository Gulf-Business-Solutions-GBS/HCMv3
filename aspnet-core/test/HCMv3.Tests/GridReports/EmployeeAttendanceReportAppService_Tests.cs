using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Attendance grid report (legacy <c>uscRptEmployeeAttendance.ascx</c>,
/// <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAttendanceReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeAttendanceReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeAttendanceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAttendanceReportAppService>();
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

    private AttendanceType CreateAttendanceType(string code)
    {
        AttendanceType attendanceType = null;
        UsingDbContext(context =>
        {
            attendanceType = context.Set<AttendanceType>().Add(new AttendanceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });

        return attendanceType;
    }

    private void CreateAttendance(
        Employee employee,
        AttendanceType attendanceType,
        DateTime start,
        DateTime end,
        double hours = 8,
        double days = 1)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAttendance>().Add(new EmployeeAttendance
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AttendanceTypeId = attendanceType.Id,
                StartDate = start,
                EndDate = end,
                StartTime = start.Date.AddHours(8),
                EndTime = start.Date.AddHours(17),
                AttendanceHours = hours,
                TotalAttendDays = days,
                Comments = "C-" + employee.Code
            });
        });
    }

    private static GetEmployeeAttendanceReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeAttendanceReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Attendance_Overlapping_The_Requested_Window()
    {
        var attendanceType = CreateAttendanceType("AT-OVL");
        var inRange = CreateEmployee("TOVL-IN");
        var before = CreateEmployee("TOVL-BEFORE");
        var after = CreateEmployee("TOVL-AFTER");

        CreateAttendance(inRange, attendanceType, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreateAttendance(before, attendanceType, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateAttendance(after, attendanceType, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeAttendanceReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TOVL-IN");
        codes.ShouldNotContain("TOVL-BEFORE");
        codes.ShouldNotContain("TOVL-AFTER");
    }

    [Fact]
    public async Task Should_Include_Attendance_Touching_The_Window_Boundary()
    {
        var attendanceType = CreateAttendanceType("AT-EDGE");
        var startsOnToDate = CreateEmployee("TEDGE-START");
        var endsOnFromDate = CreateEmployee("TEDGE-END");

        // Legacy's four-alternative overlap test includes both boundaries.
        CreateAttendance(startsOnToDate, attendanceType, new DateTime(2024, 12, 31), new DateTime(2025, 1, 5));
        CreateAttendance(endsOnFromDate, attendanceType, new DateTime(2023, 12, 20), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeAttendanceReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TEDGE-START");
        codes.ShouldContain("TEDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var attendanceType = CreateAttendanceType("AT-ST");
        var active = CreateEmployee("TST-ACTIVE");
        var inactive = CreateEmployee("TST-INACTIVE", EmployeeStatus.Inactive);

        CreateAttendance(active, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));
        CreateAttendance(inactive, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));

        var result = await _appService.GetEmployeeAttendanceReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TST-INACTIVE");
        codes.ShouldNotContain("TST-ACTIVE");
    }

    [Fact]
    public async Task Should_Project_The_Real_End_Date_Not_The_Legacy_Copy_Of_Start_Date()
    {
        var attendanceType = CreateAttendanceType("AT-PROJ");
        var employee = CreateEmployee("TPROJ-1");

        CreateAttendance(employee, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 5), 7.5, 4);

        var result = await _appService.GetEmployeeAttendanceReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "TPROJ-1"));

        var row = result.Items.Single();
        row.EmployeeName.ShouldBe("Full TPROJ-1");
        row.AttendanceTypeName.ShouldBe("AT-PROJ-En");
        row.StartDate.ShouldBe(new DateTime(2024, 3, 1));
        // Legacy line 544 filled the EndDate cell from StartDate; that defect is not reproduced.
        row.EndDate.ShouldBe(new DateTime(2024, 3, 5));
        row.AttendanceHours.ShouldBe(7.5);
        row.TotalAttendDays.ShouldBe(4);
        row.Comments.ShouldBe("C-TPROJ-1");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var attendanceType = CreateAttendanceType("AT-EXP");
        var first = CreateEmployee("TEXP-A");
        var second = CreateEmployee("TEXP-B");
        var third = CreateEmployee("TEXP-C");
        var excluded = CreateEmployee("TEXP-OUT");

        CreateAttendance(first, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));
        CreateAttendance(second, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));
        CreateAttendance(third, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));
        CreateAttendance(excluded, attendanceType, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "TEXP-A,TEXP-B,TEXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeAttendanceReportToExcel(input);

        file.FileName.ShouldBe("EmployeeAttendance.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "TEXP-A", "TEXP-B", "TEXP-C" });
        exported.First().Keys.ShouldContain("Attendance Type");
    }
}
