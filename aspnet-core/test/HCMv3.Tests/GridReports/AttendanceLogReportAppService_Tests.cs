using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Attendance Log grid report (legacy <c>uscRptAttendanceLog.ascx</c>, <c>Table1()</c>),
/// including its payroll-area filter and the "both punches or nothing" duration rule.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AttendanceLogReportAppService_Tests : AppTestBase
{
    private readonly IAttendanceLogReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public AttendanceLogReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAttendanceLogReportAppService>();
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

    private void CreateTimeRegister(
        Employee employee,
        DateTime dayDate,
        DateTime actualIn = default,
        DateTime actualOut = default,
        double workingMinutes = 0,
        double overtimeMinutes = 0,
        double absentDay = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = dayDate,
                ActualIn = actualIn,
                ActualOut = actualOut,
                ActualWorkingMins = workingMinutes,
                OvertimeMinutes = overtimeMinutes,
                AbsentDay = absentDay
            });
        });
    }

    /// <summary>Creates a PayrollArea and an org assignment covering TODAY -- what legacy filters on.</summary>
    private PayrollArea CreatePayrollAreaAssignment(Employee employee, string payrollAreaCode)
    {
        PayrollArea payrollArea = null;
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;

            context.SaveChanges();

            payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = payrollAreaCode,
                Name = "PA-" + payrollAreaCode,
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = Clock.Now.Date.AddYears(-1),
                EndDate = Clock.Now.Date.AddYears(1),
                PayrollAreaId = payrollArea.Id
            });
        });

        return payrollArea;
    }

    private static GetAttendanceLogReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 100)
    {
        return new GetAttendanceLogReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Include_The_Days_On_Both_Window_Boundaries_And_Exclude_The_Neighbours()
    {
        var employee = CreateEmployee("AL-EDGE");

        // Legacy predicate is DayDate >= dtFrom && DayDate <= dtTo -- both ends inclusive.
        CreateTimeRegister(employee, new DateTime(2024, 5, 31));
        CreateTimeRegister(employee, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, new DateTime(2024, 6, 30));
        CreateTimeRegister(employee, new DateTime(2024, 7, 1));

        var result = await _appService.GetAttendanceLogReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "AL-EDGE"));

        var days = result.Items.Select(i => i.DayDate).OrderBy(d => d).ToList();
        days.ShouldBe(new List<DateTime> { new(2024, 6, 1), new(2024, 6, 30) });
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Area_Using_Todays_Assignment()
    {
        var matching = CreateEmployee("AL-PA-MATCH");
        var other = CreateEmployee("AL-PA-OTHER");

        CreateTimeRegister(matching, new DateTime(2024, 6, 10));
        CreateTimeRegister(other, new DateTime(2024, 6, 10));

        var payrollAreaCode = "PA-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        CreatePayrollAreaAssignment(matching, payrollAreaCode);
        CreatePayrollAreaAssignment(other, payrollAreaCode + "-X");

        var result = await _appService.GetAttendanceLogReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), payrollAreaCode: payrollAreaCode));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("AL-PA-MATCH");
        codes.ShouldNotContain("AL-PA-OTHER");
    }

    [Fact]
    public async Task Should_Project_The_Legacy_Time_Columns()
    {
        var employee = CreateEmployee("AL-PROJ");

        var day = new DateTime(2024, 6, 12); // a Wednesday
        CreateTimeRegister(
            employee,
            day,
            actualIn: day.AddHours(8).AddMinutes(5),
            actualOut: day.AddHours(17).AddMinutes(35),
            workingMinutes: 510,
            overtimeMinutes: 90);

        var result = await _appService.GetAttendanceLogReport(
            Input(day, day, employeeCode: "AL-PROJ"));

        var row = result.Items.Single();
        row.EmployeeName.ShouldBe("Full AL-PROJ");
        row.SignIn.ShouldBe("08:05");
        row.SignOut.ShouldBe("17:35");
        row.WorkingHours.ShouldBe("08:30:00");
        row.OverTime.ShouldBe("01:30:00");
        // Legacy wrote DayOfWeek.ToString(), i.e. the invariant English name.
        row.WeekDay.ShouldBe("Wednesday");
    }

    [Fact]
    public async Task Should_Zero_The_Duration_Columns_When_A_Punch_Is_Missing()
    {
        var employee = CreateEmployee("AL-NOPUNCH");

        var day = new DateTime(2024, 6, 13);
        // In but never out: legacy forces BOTH duration columns to 00:00:00 regardless of the minutes
        // stored on the register (lines 617-624/629-636).
        CreateTimeRegister(
            employee,
            day,
            actualIn: day.AddHours(8),
            workingMinutes: 480,
            overtimeMinutes: 60);

        var result = await _appService.GetAttendanceLogReport(Input(day, day, employeeCode: "AL-NOPUNCH"));

        var row = result.Items.Single();
        row.SignIn.ShouldBe("08:00");
        row.SignOut.ShouldBeNull();
        row.WorkingHours.ShouldBe("00:00:00");
        row.OverTime.ShouldBe("00:00:00");
    }

    [Fact]
    public async Task Should_Report_Absent_In_The_Status_Column_When_The_Day_Has_No_Leave()
    {
        var absent = CreateEmployee("AL-ABS");
        var present = CreateEmployee("AL-PRESENT");

        var day = new DateTime(2024, 6, 14);
        CreateTimeRegister(absent, day, absentDay: 1);
        CreateTimeRegister(present, day, absentDay: 0);

        var result = await _appService.GetAttendanceLogReport(
            Input(day, day, employeeCode: "AL-ABS,AL-PRESENT"));

        result.Items.Single(i => i.EmployeeCode == "AL-ABS").Status.ShouldBe("Absent");
        result.Items.Single(i => i.EmployeeCode == "AL-PRESENT").Status.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("AL-EXP");

        CreateTimeRegister(employee, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, new DateTime(2024, 6, 2));
        CreateTimeRegister(employee, new DateTime(2024, 6, 3));
        // Outside the window -- must not reach the export.
        CreateTimeRegister(employee, new DateTime(2024, 7, 1));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetAttendanceLogReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "AL-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("AttendanceLog.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("AL-HDR");
        CreateTimeRegister(employee, new DateTime(2024, 6, 5));

        var file = await _appService.GetAttendanceLogReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "AL-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
