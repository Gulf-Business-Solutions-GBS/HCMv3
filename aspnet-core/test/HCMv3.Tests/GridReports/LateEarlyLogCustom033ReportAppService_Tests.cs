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
/// Tests for the Late/Early Log (Custom 033) grid report -- legacy
/// <c>uscRptLateEarlyLog_Custom033.ascx.cs</c>, <c>Table1()</c>.
///
/// This is the leanest report of the family and the crudest: it answers only "when did this person arrive
/// and leave, and for how long". The tests here pin the three legacy defects that make it behave unlike
/// every other variant -- the hard-coded night shift, the ignored time-management status and the
/// transaction-type-blind punch selection -- plus the unreachable "*" status branch.
///
/// The algorithm is shared with <c>uscRptLateEarlyLog_Custom036</c>; see
/// <see cref="LateEarlyLogCustom036ReportAppService_Tests"/> for the tests that pin the one difference
/// between them.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom033ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    private readonly ILateEarlyLogCustom033ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom033ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom033ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom033ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 1000)
    {
        return new GetLateEarlyLogCustom033ReportInput
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
        var employee = CreateEmployee("C033-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        foreach (var day in new[] { from.AddDays(-1), from, to, to.AddDays(1) })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(from, to, "C033-BOUND"));

        result.Items.Count.ShouldBe(3);
        result.Items.Select(i => i.Date.Date).ShouldBe(new[] { from, from.AddDays(1), to }, ignoreOrder: true);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. <c>isNightShift = true; //isNightShift = duty.IsNightShift;</c> -- every day
    /// is treated as straddling midnight, so the punch window always extends into the FOLLOWING calendar
    /// day regardless of the duty's real flag. A punch made after midnight on a plain day shift is
    /// therefore still selected, which no other variant would do.
    /// </summary>
    [Fact]
    public async Task Should_Treat_Every_Duty_As_A_Night_Shift_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C033-NIGHT");
        var day = new DateTime(2024, 6, 10);

        // A plain DAY shift: IsNightShift is false, and the flexi window is 08:00-16:00.
        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16), isNightShift: false);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        // 10:00 the NEXT morning. With a correct night-shift flag the window would end at 16:00 today and
        // this punch would be invisible; the hard-coded flag pushes the window to 16:00 TOMORROW instead.
        CreatePunch(employee, day.AddDays(1), TimeSpan.FromHours(10), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-NIGHT"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(8));
        row.SignOut.ShouldBe(TimeSpan.FromHours(10));
        // 08:00 today to 10:00 tomorrow.
        row.TotalHours.ShouldBe(TimeSpan.FromHours(26));
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. Punch selection here ignores the transaction type entirely: both candidate
    /// lists are the same unfiltered set. "Sign In" is simply the EARLIEST event in the window, so a
    /// clock-OUT punch can become the Sign In.
    /// </summary>
    [Fact]
    public async Task Should_Select_Sign_In_Ignoring_The_Transaction_Type_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C033-TYPE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        // The earliest event of the day is a clock-OUT. Every other variant filters on TransType and would
        // ignore it for the Sign In; this one does not.
        CreatePunch(employee, day, TimeSpan.FromHours(9), PunchOutCode);
        CreatePunch(employee, day, TimeSpan.FromHours(15), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-TYPE"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(9));
        row.SignOut.ShouldBe(TimeSpan.FromHours(15));
    }

    /// <summary>
    /// The time policy's management status is IGNORED: even a policy that is not FlexiTime selects punches
    /// through the duty's flexi window, and the clock-in/clock-out limits are never consulted. A punch
    /// well inside the generous clock-in limit but OUTSIDE the flexi window is therefore not selected.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_The_Time_Management_Status_And_Use_The_Flexi_Window()
    {
        var employee = CreateEmployee("C033-FLEXI");
        var day = new DateTime(2024, 6, 10);

        // Shift 08:00-16:00 but a NARROW flexi window of 10:00-11:00.
        var dutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            flexiStartTime: TimeSpan.FromHours(10),
            flexiEndTime: TimeSpan.FromHours(11));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        // A NON-flexi policy with a four-hour clock-in limit, which this report never reads.
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        // 08:00 is comfortably inside the clock-in limit but BEFORE the flexi window opens.
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(10.5), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-FLEXI"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(10.5));
    }

    /// <summary>
    /// Sign Out is the latest event that is not at the VERY SAME INSTANT as the Sign In, so a day with
    /// exactly one punch reports a Sign In and no Sign Out -- and Total Hours of zero, written rather than
    /// left blank.
    /// </summary>
    [Fact]
    public async Task Should_Report_No_Sign_Out_And_Zero_Total_Hours_For_A_Single_Punch()
    {
        var employee = CreateEmployee("C033-ONE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreatePunch(employee, day, TimeSpan.FromHours(9), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-ONE"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(9));
        row.SignOut.ShouldBeNull();
        // Written as zero, not left blank -- unlike every other variant.
        row.TotalHours.ShouldBe(TimeSpan.Zero);
    }

    /// <summary>
    /// UNREACHABLE LEGACY BRANCH PINNED. Legacy writes "*" when exactly one of its two candidate lists is
    /// non-empty, but both lists are built from IDENTICAL predicates, so the branch can never fire. This
    /// test asserts it never does, whether the day has punches or not.
    /// </summary>
    [Fact]
    public async Task Should_Never_Report_The_Unreachable_Asterisk_Status()
    {
        var withPunch = CreateEmployee("C033-STAR1");
        var withoutPunch = CreateEmployee("C033-STAR2");
        var day = new DateTime(2024, 6, 10);

        foreach (var employee in new[] { withPunch, withoutPunch })
        {
            GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        }

        CreatePunch(withPunch, day, TimeSpan.FromHours(9), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom033Report(
            Input(day, day, "C033-STAR1,C033-STAR2"));

        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.Status != "*");
        result.Items.ShouldAllBe(i => i.Status == "");
    }

    /// <summary>
    /// DATA-LOSS DEFECT DOCUMENTED. Legacy's full-day-leave branch writes a "Leave" status but does not
    /// short-circuit, and the normal-attendance block that follows overwrites Status unconditionally. No
    /// user ever saw the marker, so a full-day leave is indistinguishable from a day with no punches. The
    /// port matches the OBSERVABLE legacy output; this test records that, so the loss is visible rather
    /// than merely absent.
    /// </summary>
    [Fact]
    public async Task Should_Not_Mark_A_Full_Day_Leave_Because_The_Legacy_Branch_Is_Dead()
    {
        var employee = CreateEmployee("C033-LEAVE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreateAbsence(employee, CreateAbsenceType(), day, day);

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-LEAVE"));

        var row = result.Items.ShouldHaveSingleItem();
        row.Status.ShouldBe("");
    }

    /// <summary>
    /// An employee with no time policy covering the day produces NO ROW at all for that day.
    /// </summary>
    [Fact]
    public async Task Should_Skip_A_Day_With_No_Time_Policy()
    {
        var employee = CreateEmployee("C033-NOPOL");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        // Policy covers only the first week; the report day falls after it.
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 5));

        var result = await _appService.GetLateEarlyLogCustom033Report(Input(day, day, "C033-NOPOL"));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// The export must honour the filters and return EVERY matching row, ignoring the page size.
    /// </summary>
    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C033-EXP");
        var from = new DateTime(2024, 6, 1);
        var to = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom033ReportToExcel(
            Input(from, to, "C033-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom033.xlsx");
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
        var employee = CreateEmployee("C033-HDR");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom033ReportToExcel(Input(day, day, "C033-HDR"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// The exact column set legacy declares -- 22 columns, including the two access-unit columns that are
    /// the whole of Custom036's difference from this report, and excluding the many columns legacy
    /// commented out.
    /// </summary>
    [Fact]
    public async Task Should_Export_Exactly_The_Legacy_Column_Set()
    {
        var employee = CreateEmployee("C033-COLS");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom033ReportToExcel(Input(day, day, "C033-COLS"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.Count.ShouldBe(22);
        headers.ShouldContain("Punch In Access Unit");
        headers.ShouldContain("Punch Out Access Unit");

        // Commented out in the legacy control -- must not reappear.
        foreach (var dropped in new[]
                 {
                     "Early In", "Early Out", "Late In", "Late Out",
                     "Planned Hours", "Overtime Hours", "Absence Hours",
                     "Leave Type", "Partial Leave Hours", "Time Policy",
                     "Punch In Location", "Punch Out Location"
                 })
        {
            headers.ShouldNotContain(dropped);
        }
    }
}
