using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Late/Early Log (Custom 023) grid report -- legacy
/// <c>uscRptLateEarlyLog_Custom023.ascx.cs</c>, <c>Table1()</c>.
///
/// What distinguishes this variant from the base control, and what these tests pin: early/late deltas
/// computed from TIMES OF DAY rather than timestamps, Planned Hours derived from the raw shift times with
/// a night-shift inversion, a five-rung status ladder that adds "Rest Day", "*" and "Absent", the total
/// suppression of punch reporting on a week off, and a public holiday that blanks every hour column.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom023ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    private readonly ILateEarlyLogCustom023ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom023ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom023ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom023ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 1000)
    {
        return new GetLateEarlyLogCustom023ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    /// <summary>
    /// The window is INCLUSIVE at both ends and produces exactly one row per covered day; days either
    /// side of it must not appear.
    /// </summary>
    [Fact]
    public async Task Should_Include_Both_Boundary_Days_And_Nothing_Outside_The_Window()
    {
        var employee = CreateEmployee("C023-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        foreach (var day in new[] { from.AddDays(-1), from, to, to.AddDays(1) })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(from, to, "C023-BOUND"));

        result.Items.Count.ShouldBe(3);
        result.Items.Select(i => i.Date.Date).ShouldBe(new[] { from, from.AddDays(1), to }, ignoreOrder: true);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The shift locals live OUTSIDE both loops in legacy and are only reassigned
    /// when a Duty resolves, so a day with no Duty reports the PREVIOUS day's shift bounds.
    /// </summary>
    [Fact]
    public async Task Should_Carry_Previous_Shift_Bounds_Into_A_Day_Without_Duty_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C023-STICKY");
        var from = new DateTime(2024, 6, 10); // Monday
        var to = new DateTime(2024, 6, 11);   // Tuesday -- deliberately left without a duty.

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        var dutyPeriodId = CreateDutyPeriodMissingOneWeekday(dutyId, DayOfWeek.Tuesday);
        AssignDutyPeriod(employee, dutyPeriodId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(from, to, "C023-STICKY"));

        var tuesday = result.Items.Single(i => i.Date.Date == to);

        tuesday.ShiftStart.ShouldBe(TimeSpan.FromHours(8));
        tuesday.ShiftEnd.ShouldBe(TimeSpan.FromHours(16));
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The base control and Custom019 both fall back to reporting Total Hours on a
    /// public holiday. Custom023 has no such branch, so a public holiday reports NO Planned, Total or
    /// Absence hours at all even though the employee worked a full day.
    /// </summary>
    [Fact]
    public async Task Should_Report_No_Hours_At_All_On_A_Public_Holiday_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C023-PH");
        var holiday = new DateTime(2024, 6, 10);
        var normalDay = new DateTime(2024, 6, 11);

        var calendarId = CreateHolidayCalendarWithHoliday(holiday);
        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        AssignDutyPeriod(
            employee,
            CreateDutyPeriod(dutyId, calendarId),
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        foreach (var day in new[] { holiday, normalDay })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom023Report(
            Input(holiday, normalDay, "C023-PH"));

        var holidayRow = result.Items.Single(i => i.Date.Date == holiday);
        var normalRow = result.Items.Single(i => i.Date.Date == normalDay);

        holidayRow.Status.ShouldBe("Public Holiday");
        // Eight hours actually worked, and every hour column is blank. That is the defect.
        holidayRow.TotalHours.ShouldBeNull();
        holidayRow.PlannedHours.ShouldBeNull();
        holidayRow.AbsenceHours.ShouldBeNull();

        // The same day's work on a NON-holiday is reported normally, so the assertion above is meaningful.
        normalRow.TotalHours.ShouldBe(TimeSpan.FromHours(8));
        normalRow.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
    }

    /// <summary>
    /// Custom023 computes early/late from TIMES OF DAY, not from the two full timestamps. On a night
    /// shift, where the base control would see a punch-out on the following morning as a small delta from
    /// the previous evening's shift end, this variant sees the raw clock difference instead.
    /// </summary>
    [Fact]
    public async Task Should_Compute_Late_In_From_The_Time_Of_Day_Not_The_Timestamp()
    {
        var employee = CreateEmployee("C023-TOD");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(9.5), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(15), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(day, day, "C023-TOD"));

        var row = result.Items.ShouldHaveSingleItem();
        row.LateIn.ShouldBe(TimeSpan.FromHours(1.5));
        row.EarlyIn.ShouldBeNull();
        row.EarlyOut.ShouldBe(TimeSpan.FromHours(1));
        row.LateOut.ShouldBeNull();
    }

    /// <summary>
    /// The status ladder rung the base control does not have: a day with no punches at all reads
    /// "Absent" -- an English-only literal even in the Arabic aspect, reproduced verbatim.
    /// </summary>
    [Fact]
    public async Task Should_Report_Absent_When_The_Day_Has_No_Punches()
    {
        var employee = CreateEmployee("C023-ABS");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(day, day, "C023-ABS"));

        result.Items.ShouldHaveSingleItem().Status.ShouldBe("Absent");
    }

    /// <summary>
    /// A duty flagged <c>IsRestDay</c> reads "Rest Day" -- a rung neither the base control nor Custom019
    /// has. It outranks the public-holiday rung but is outranked by the week-off rung.
    /// </summary>
    [Fact]
    public async Task Should_Report_Rest_Day_For_A_Rest_Day_Duty()
    {
        var employee = CreateEmployee("C023-REST");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16), isRestDay: true);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(day, day, "C023-REST"));

        result.Items.ShouldHaveSingleItem().Status.ShouldBe("Rest Day");
    }

    /// <summary>
    /// Unlike the base control and Custom019, Custom023 gates the WHOLE punch block on
    /// <c>!isWeekOff</c>, so a punch made on a week off is not reported at all -- neither the Sign In nor
    /// the resulting Total Hours.
    /// </summary>
    [Fact]
    public async Task Should_Suppress_Punches_Entirely_On_A_Week_Off()
    {
        var employee = CreateEmployee("C023-WO");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16), isWeeklyOff: true);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(day, day, "C023-WO"));

        var row = result.Items.ShouldHaveSingleItem();
        row.Status.ShouldBe("Week Off");
        row.SignIn.ShouldBeNull();
        row.SignOut.ShouldBeNull();
        row.TotalHours.ShouldBeNull();
    }

    /// <summary>
    /// A full-day leave short-circuits the day: the row carries the Leave status, the absence type and a
    /// full shift's worth of Absence Hours -- unlike Custom019, which zeroes them.
    /// </summary>
    [Fact]
    public async Task Should_Report_A_Full_Shift_Of_Absence_Hours_On_A_Full_Day_Leave()
    {
        var employee = CreateEmployee("C023-LEAVE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreateAbsence(employee, CreateAbsenceType(), day, day);

        var result = await _appService.GetLateEarlyLogCustom023Report(Input(day, day, "C023-LEAVE"));

        var row = result.Items.ShouldHaveSingleItem();
        row.Status.ShouldBe("Leave");
        row.LeaveType.ShouldBe("Annual");
        row.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
        row.AbsenceHours.ShouldBe(TimeSpan.FromHours(8));
        row.TotalHours.ShouldBe(TimeSpan.Zero);
    }

    /// <summary>
    /// The export must honour the filters and return EVERY matching row, ignoring the page size.
    /// </summary>
    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C023-EXP");
        var from = new DateTime(2024, 6, 1);
        var to = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom023ReportToExcel(
            Input(from, to, "C023-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom023.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        CountExportedRows(bytes).ShouldBe(5);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler and to a source grep.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C023-HDR");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom023ReportToExcel(Input(day, day, "C023-HDR"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// Custom023 is the base column set MINUS the three break columns. Pin that so a future edit cannot
    /// quietly reintroduce them, given this report has no break machinery to fill them.
    /// </summary>
    [Fact]
    public async Task Should_Not_Export_The_Three_Break_Columns()
    {
        var employee = CreateEmployee("C023-COLS");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom023ReportToExcel(Input(day, day, "C023-COLS"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        // Sanity first, so the ShouldNotContain assertions cannot pass vacuously.
        headers.Count.ShouldBe(34);
        headers.ShouldContain("Access Id");
        headers.ShouldContain("Org Level 1");

        headers.ShouldNotContain("Break Hours");
        headers.ShouldNotContain("Break In");
        headers.ShouldNotContain("Break Out");
    }
}
