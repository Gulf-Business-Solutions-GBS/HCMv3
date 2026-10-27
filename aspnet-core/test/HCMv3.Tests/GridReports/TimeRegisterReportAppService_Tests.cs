using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
/// Tests for the Time Register grid report (legacy <c>uscRptTimeRegister.ascx</c>, <c>Table1()</c>),
/// including the "duty period required" row filter, the weekend-duty fallback that legacy substitutes
/// for every empty time/minutes cell, and the total-late arithmetic.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeRegisterReportAppService_Tests : AppTestBase
{
    private readonly ITimeRegisterReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public TimeRegisterReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITimeRegisterReportAppService>();
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

    private Duty CreateDuty(string code)
    {
        Duty duty = null;
        UsingDbContext(context =>
        {
            duty = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            }).Entity;
        });

        return duty;
    }

    /// <summary>
    /// A duty period is MANDATORY for a row to appear at all: legacy line 551 filters on it. The optional
    /// Friday/Saturday duties feed the weekend-duty fallback the legacy formatter uses for empty cells.
    /// </summary>
    private DutyPeriod CreateDutyPeriod(string code, Duty fridayDuty = null, Duty saturdayDuty = null)
    {
        DutyPeriod dutyPeriod = null;
        UsingDbContext(context =>
        {
            dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                FridayDutyId = fridayDuty?.Id,
                SaturdayDutyId = saturdayDuty?.Id
            }).Entity;
        });

        return dutyPeriod;
    }

    private void CreateTimeRegister(
        Employee employee,
        DutyPeriod dutyPeriod,
        DateTime dayDate,
        DateTime shiftStart = default,
        DateTime actualStart = default,
        DateTime shiftEnd = default,
        DateTime actualEnd = default,
        double actualWorkingMins = 0,
        double lateIn = 0,
        double deductionLateIn = 0,
        double earlyOut = 0,
        double deductionEarlyOut = 0,
        double dedAfterMonthlyTol = 0,
        double absentDay = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DutyPeriodId = dutyPeriod?.Id,
                DayDate = dayDate,
                ShiftStartTime1 = shiftStart,
                ActualStartTime1 = actualStart,
                ShiftEndTime1 = shiftEnd,
                ActualEndTime1 = actualEnd,
                ActualWorkingMins = actualWorkingMins,
                LateIn = lateIn,
                DeductionLateIn = deductionLateIn,
                EarlyOut = earlyOut,
                DeductionEarlyOut = deductionEarlyOut,
                DedAfterMonthlyTol = dedAfterMonthlyTol,
                AbsentDay = absentDay
            });
        });
    }

    private static GetTimeRegisterReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetTimeRegisterReportInput
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
    public async Task Should_Include_The_Days_On_Both_Window_Boundaries_And_Exclude_The_Neighbours()
    {
        var employee = CreateEmployee("TR-EDGE");
        var dutyPeriod = CreateDutyPeriod("TR-DP-EDGE");

        // Legacy predicate is DayDate >= dtFrom && DayDate <= dtTo -- both ends inclusive.
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 5, 31));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 30));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 7, 1));

        var result = await _appService.GetTimeRegisterReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TR-EDGE"));

        var days = result.Items.Select(i => i.DayDate).OrderBy(d => d).ToList();
        days.ShouldBe(new List<DateTime> { new(2024, 6, 1), new(2024, 6, 30) });
    }

    /// <summary>Legacy line 551 requires a duty period; a register row without one is simply not a row.</summary>
    [Fact]
    public async Task Should_Exclude_Register_Rows_That_Have_No_Duty_Period()
    {
        var withDutyPeriod = CreateEmployee("TR-DP-YES");
        var withoutDutyPeriod = CreateEmployee("TR-DP-NO");
        var dutyPeriod = CreateDutyPeriod("TR-DP-REQ");

        var day = new DateTime(2024, 6, 10);
        CreateTimeRegister(withDutyPeriod, dutyPeriod, day);
        CreateTimeRegister(withoutDutyPeriod, null, day);

        var result = await _appService.GetTimeRegisterReport(
            Input(day, day, employeeCode: "TR-DP-YES,TR-DP-NO"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TR-DP-YES");
        codes.ShouldNotContain("TR-DP-NO");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var active = CreateEmployee("TR-ST-ACTIVE");
        var inactive = CreateEmployee("TR-ST-INACTIVE", EmployeeStatus.Inactive);
        var dutyPeriod = CreateDutyPeriod("TR-DP-ST");

        var day = new DateTime(2024, 6, 11);
        CreateTimeRegister(active, dutyPeriod, day);
        CreateTimeRegister(inactive, dutyPeriod, day);

        var result = await _appService.GetTimeRegisterReport(Input(day, day, status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TR-ST-INACTIVE");
        codes.ShouldNotContain("TR-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Project_Times_Working_Hours_And_Total_Late_Minutes()
    {
        var employee = CreateEmployee("TR-PROJ");
        var dutyPeriod = CreateDutyPeriod("TR-DP-PROJ");

        var day = new DateTime(2024, 6, 12); // a Wednesday: no weekend-duty fallback applies
        CreateTimeRegister(
            employee,
            dutyPeriod,
            day,
            shiftStart: day.AddHours(8),
            actualStart: day.AddHours(8).AddMinutes(15),
            shiftEnd: day.AddHours(17),
            actualEnd: day.AddHours(16).AddMinutes(30),
            actualWorkingMins: 495,
            lateIn: 15,
            deductionLateIn: 15,
            earlyOut: 30,
            deductionEarlyOut: 30,
            dedAfterMonthlyTol: 10);

        var result = await _appService.GetTimeRegisterReport(Input(day, day, employeeCode: "TR-PROJ"));

        var row = result.Items.Single();
        // LEGACY FIDELITY (line 583): this column is FullNameEn unconditionally, never the Arabic name.
        row.EmployeeName.ShouldBe("Full TR-PROJ");
        row.ShiftStart.ShouldBe(day.AddHours(8).ToString("t", CultureInfo.CurrentUICulture));
        row.ActualSignIn.ShouldBe(day.AddHours(8).AddMinutes(15).ToString("t", CultureInfo.CurrentUICulture));
        row.ShiftEnd.ShouldBe(day.AddHours(17).ToString("t", CultureInfo.CurrentUICulture));
        row.ActualSignOut.ShouldBe(day.AddHours(16).AddMinutes(30).ToString("t", CultureInfo.CurrentUICulture));
        // Legacy line 617: minutes / 60 rounded to two decimals.
        row.TotalWorkingHours.ShouldBe(8.25);
        // Legacy lines 638-640: late + early when BOTH are non-zero.
        row.TotalLateMinutes.ShouldBe(45);
        row.DeductionAfterMonthlyTolerance.ShouldBe("10");
    }

    /// <summary>
    /// Legacy lines 638-640 add the two only when BOTH are non-zero; with one of them zero the other is
    /// returned on its own, and with neither the column is 0.
    /// </summary>
    [Fact]
    public async Task Should_Return_The_Single_Non_Zero_Value_As_Total_Late_Minutes()
    {
        var lateOnly = CreateEmployee("TR-LATE-ONLY");
        var earlyOnly = CreateEmployee("TR-EARLY-ONLY");
        var neither = CreateEmployee("TR-NEITHER");
        var dutyPeriod = CreateDutyPeriod("TR-DP-LATE");

        var day = new DateTime(2024, 6, 13);
        CreateTimeRegister(lateOnly, dutyPeriod, day, lateIn: 20);
        CreateTimeRegister(earlyOnly, dutyPeriod, day, earlyOut: 35);
        CreateTimeRegister(neither, dutyPeriod, day);

        var result = await _appService.GetTimeRegisterReport(
            Input(day, day, employeeCode: "TR-LATE-ONLY,TR-EARLY-ONLY,TR-NEITHER"));

        result.Items.Single(i => i.EmployeeCode == "TR-LATE-ONLY").TotalLateMinutes.ShouldBe(20);
        result.Items.Single(i => i.EmployeeCode == "TR-EARLY-ONLY").TotalLateMinutes.ShouldBe(35);
        result.Items.Single(i => i.EmployeeCode == "TR-NEITHER").TotalLateMinutes.ShouldBe(0);
    }

    /// <summary>
    /// Legacy lines 597-641: on a Friday or Saturday every EMPTY time/minutes cell is replaced by the
    /// duty period's Friday/Saturday duty NAME rather than left blank. The deduction-after-monthly-
    /// tolerance column is the one exception (line 643) -- it stays empty.
    /// </summary>
    [Fact]
    public async Task Should_Substitute_The_Weekend_Duty_Name_For_Empty_Cells_On_A_Friday()
    {
        var employee = CreateEmployee("TR-FRI");
        var fridayDuty = CreateDuty("TR-FRI-DUTY");
        var dutyPeriod = CreateDutyPeriod("TR-DP-FRI", fridayDuty: fridayDuty);

        var friday = new DateTime(2024, 6, 14); // a Friday, with no punches and no minutes at all
        CreateTimeRegister(employee, dutyPeriod, friday);

        var result = await _appService.GetTimeRegisterReport(Input(friday, friday, employeeCode: "TR-FRI"));

        var row = result.Items.Single();
        row.ShiftStart.ShouldBe("TR-FRI-DUTY-En");
        row.ActualSignIn.ShouldBe("TR-FRI-DUTY-En");
        row.ShiftEnd.ShouldBe("TR-FRI-DUTY-En");
        row.ActualSignOut.ShouldBe("TR-FRI-DUTY-En");
        row.LateIn.ShouldBe("TR-FRI-DUTY-En");
        row.DeductionLateIn.ShouldBe("TR-FRI-DUTY-En");
        row.EarlyOut.ShouldBe("TR-FRI-DUTY-En");
        row.DeductionEarlyOut.ShouldBe("TR-FRI-DUTY-En");
        // Legacy line 643 gives this column NO duty-name fallback.
        row.DeductionAfterMonthlyTolerance.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Legacy would have thrown a NullReferenceException here (it dereferenced FridayDuty.NameEn without
    /// a null check). That is a genuine crash, not a data semantic, so the port returns an empty string --
    /// what an absent value means in every other cell on the row.
    /// </summary>
    [Fact]
    public async Task Should_Fall_Back_To_Empty_String_When_The_Duty_Period_Has_No_Friday_Duty()
    {
        var employee = CreateEmployee("TR-FRI-NULL");
        var dutyPeriod = CreateDutyPeriod("TR-DP-FRI-NULL");

        var friday = new DateTime(2024, 6, 14);
        CreateTimeRegister(employee, dutyPeriod, friday);

        var result = await _appService.GetTimeRegisterReport(Input(friday, friday, employeeCode: "TR-FRI-NULL"));

        var row = result.Items.Single();
        row.ShiftStart.ShouldBe(string.Empty);
        row.LateIn.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var employee = CreateEmployee("TR-PAGE");
        var dutyPeriod = CreateDutyPeriod("TR-DP-PAGE");

        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 2));

        var page = await _appService.GetTimeRegisterReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TR-PAGE", maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        // Default sort is EmployeeCode ASC, DayDate ASC.
        page.Items.Single().DayDate.ShouldBe(new DateTime(2024, 6, 1));
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("TR-EXP");
        var dutyPeriod = CreateDutyPeriod("TR-DP-EXP");

        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 2));
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 3));
        // Outside the window -- must not reach the export.
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 7, 1));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetTimeRegisterReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TR-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("TimeRegister.xlsx");
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
        var employee = CreateEmployee("TR-HDR");
        var dutyPeriod = CreateDutyPeriod("TR-DP-HDR");
        CreateTimeRegister(employee, dutyPeriod, new DateTime(2024, 6, 5));

        var file = await _appService.GetTimeRegisterReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TR-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
