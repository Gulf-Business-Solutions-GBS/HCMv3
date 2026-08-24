using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Late/Early Log (Custom 019) grid report -- legacy
/// <c>uscRptLateEarlyLog_Custom019.ascx.cs</c>, <c>Table1()</c>.
///
/// The distinguishing features under test are the three-mode <c>cmbVarient</c> combo (Detail / Summary /
/// Summary filtered to Late In over three hours), the department/division/section org filters that replace
/// the base control's payroll area, and the two legacy defects this variant carries that the base does
/// not: the sticky shift bounds and the zeroed absence hours on an unpaid full-day leave.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom019ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    private readonly ILateEarlyLogCustom019ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom019ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom019ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom019ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        LateEarlyLogCustom019Variant variant = LateEarlyLogCustom019Variant.Detail,
        string departmentCode = null,
        int maxResultCount = 1000)
    {
        return new GetLateEarlyLogCustom019ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            Variant = variant,
            DepartmentCode = departmentCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    /// <summary>
    /// The window is INCLUSIVE at both ends and produces exactly one row per covered day. A day one
    /// before and one after the window must not appear, whatever punches exist on them.
    /// </summary>
    [Fact]
    public async Task Should_Include_Both_Boundary_Days_And_Nothing_Outside_The_Window()
    {
        var employee = CreateEmployee("C019-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        // Punches on the day before the window, on both boundary days and on the day after.
        foreach (var day in new[] { from.AddDays(-1), from, to, to.AddDays(1) })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom019Report(Input(from, to, "C019-BOUND"));

        result.Items.Count.ShouldBe(3);
        result.Items.Select(i => i.Date.Value.Date).ShouldBe(
            new[] { from, from.AddDays(1), to }, ignoreOrder: true);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The shift locals live OUTSIDE both loops in legacy and are only reassigned
    /// when a Duty resolves, so a day with no Duty reports the PREVIOUS day's shift bounds instead of
    /// nothing. Faithfulness is the standing rule for this wave, so the port reproduces it.
    /// </summary>
    [Fact]
    public async Task Should_Carry_Previous_Shift_Bounds_Into_A_Day_Without_Duty_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C019-STICKY");
        var from = new DateTime(2024, 6, 10); // Monday
        var to = new DateTime(2024, 6, 11);   // Tuesday -- deliberately left without a duty.

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        var dutyPeriodId = CreateDutyPeriodMissingOneWeekday(dutyId, DayOfWeek.Tuesday);
        AssignDutyPeriod(employee, dutyPeriodId, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var result = await _appService.GetLateEarlyLogCustom019Report(Input(from, to, "C019-STICKY"));

        var tuesday = result.Items.Single(i => i.Date.Value.Date == to);

        // The defect: Tuesday has no duty at all, yet it reports Monday's 08:00-16:00 bounds.
        tuesday.ShiftStart.ShouldBe(TimeSpan.FromHours(8));
        tuesday.ShiftEnd.ShouldBe(TimeSpan.FromHours(16));
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The base control assigns <c>absencehours = plannedHours</c> before writing
    /// the Absence Hours cell on a full-day leave. Custom019 deleted that assignment but KEPT the write,
    /// so an UNPAID full-day leave reports 00:00:00 rather than the planned shift length. A PAID leave
    /// writes nothing at all.
    /// </summary>
    [Fact]
    public async Task Should_Report_Zero_Absence_Hours_On_An_Unpaid_Full_Day_Leave_Reproducing_Legacy_Defect()
    {
        var unpaid = CreateEmployee("C019-UNPAID");
        var paid = CreateEmployee("C019-PAID");
        var day = new DateTime(2024, 6, 10);

        foreach (var employee in new[] { unpaid, paid })
        {
            GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        }

        CreateAbsence(unpaid, CreateAbsenceType(isPaid: false), day, day);
        CreateAbsence(paid, CreateAbsenceType(isPaid: true), day, day);

        var result = await _appService.GetLateEarlyLogCustom019Report(
            Input(day, day, "C019-UNPAID,C019-PAID"));

        var unpaidRow = result.Items.Single(i => i.EmployeeCode == "C019-UNPAID");
        var paidRow = result.Items.Single(i => i.EmployeeCode == "C019-PAID");

        unpaidRow.Status.ShouldBe("Leave");
        // Eight planned hours, and yet the absence is reported as zero. That is the defect.
        unpaidRow.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
        unpaidRow.AbsenceHours.ShouldBe(TimeSpan.Zero);

        paidRow.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
        paidRow.AbsenceHours.ShouldBeNull();
    }

    /// <summary>
    /// The Summary variant collapses the detail rows to one per (employee, shift) and SUMS the four
    /// early/late durations and Total Hours.
    /// </summary>
    [Fact]
    public async Task Should_Sum_Durations_Per_Employee_And_Shift_In_The_Summary_Variant()
    {
        var employee = CreateEmployee("C019-SUM");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 11);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        // One hour late in on each of two days, leaving on time both days.
        foreach (var day in new[] { from, to })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(9), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var detail = await _appService.GetLateEarlyLogCustom019Report(Input(from, to, "C019-SUM"));
        detail.Items.Count.ShouldBe(2);
        detail.Items.ShouldAllBe(i => i.LateIn == TimeSpan.FromHours(1));

        var summary = await _appService.GetLateEarlyLogCustom019Report(
            Input(from, to, "C019-SUM", LateEarlyLogCustom019Variant.Summary));

        var row = summary.Items.ShouldHaveSingleItem();
        row.LateIn.ShouldBe(TimeSpan.FromHours(2));
        row.TotalHours.ShouldBe(TimeSpan.FromHours(14));
        // The summary table has no Date column -- legacy's sumTable does not declare one.
        row.Date.ShouldBeNull();
    }

    /// <summary>
    /// "Summary (Late In &gt; 3 Hours)" keeps only groups whose SUMMED Late In exceeds three hours --
    /// strictly greater, so a group at exactly three hours is dropped.
    /// </summary>
    [Fact]
    public async Task Should_Keep_Only_Groups_Over_Three_Hours_Late_In_The_Filtered_Summary_Variant()
    {
        var chronic = CreateEmployee("C019-LATE");
        var borderline = CreateEmployee("C019-EDGE");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        foreach (var employee in new[] { chronic, borderline })
        {
            GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        }

        // Chronic: 3 x 1.5h late = 4.5h. Borderline: 3 x 1h late = exactly 3h, which must NOT survive.
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            CreatePunch(chronic, day, TimeSpan.FromHours(9.5), PunchInCode);
            CreatePunch(chronic, day, TimeSpan.FromHours(16), PunchOutCode);

            CreatePunch(borderline, day, TimeSpan.FromHours(9), PunchInCode);
            CreatePunch(borderline, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom019Report(Input(
            from, to, "C019-LATE,C019-EDGE", LateEarlyLogCustom019Variant.SummaryLateInOverThreeHours));

        var row = result.Items.ShouldHaveSingleItem();
        row.EmployeeCode.ShouldBe("C019-LATE");
        row.LateIn.ShouldBe(TimeSpan.FromHours(4.5));
    }

    /// <summary>
    /// A department code that matches no org unit must select NOTHING, exactly as legacy's
    /// <c>OrgLevel2.Code == x</c> did -- not "everything", which is what silently dropping the filter
    /// would mean.
    /// </summary>
    [Fact]
    public async Task Should_Return_Nothing_When_The_Department_Filter_Matches_No_Org_Unit()
    {
        var employee = CreateEmployee("C019-DEPT");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);

        var unfiltered = await _appService.GetLateEarlyLogCustom019Report(Input(day, day, "C019-DEPT"));
        unfiltered.Items.Count.ShouldBe(1);

        var filtered = await _appService.GetLateEarlyLogCustom019Report(
            Input(day, day, "C019-DEPT", departmentCode: "NO-SUCH-DEPARTMENT"));

        filtered.TotalCount.ShouldBe(0);
        filtered.Items.ShouldBeEmpty();
    }

    /// <summary>
    /// The export must honour the filters and return EVERY matching row, ignoring the page size -- the
    /// legacy ASPxGridViewExporter exported the whole filtered set, and a page-only export would be
    /// silent data loss.
    /// </summary>
    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C019-EXP");
        var from = new DateTime(2024, 6, 1);
        var to = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        // Page size of 1 -- the export must ignore it and still return all five days.
        var file = await _appService.GetLateEarlyLogCustom019ReportToExcel(
            Input(from, to, "C019-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom019.xlsx");
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
        var employee = CreateEmployee("C019-HDR");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);

        var file = await _appService.GetLateEarlyLogCustom019ReportToExcel(Input(day, day, "C019-HDR"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// The summary variant exports a DIFFERENT, narrower column list, so its headers need checking too.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header_In_The_Summary_Variant()
    {
        var employee = CreateEmployee("C019-HDR2");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);

        var file = await _appService.GetLateEarlyLogCustom019ReportToExcel(
            Input(day, day, "C019-HDR2", LateEarlyLogCustom019Variant.Summary));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
        // Legacy's sumTable declares nine columns; the Oid sequence column is not exported.
        headers.Count.ShouldBe(9);
    }

    /// <summary>
    /// Custom019 drops eight of the base control's columns. The export list must not smuggle them back --
    /// most importantly AccessId, OrgLevel1 and the three break columns.
    /// </summary>
    [Fact]
    public async Task Should_Not_Export_The_Columns_This_Variant_Drops()
    {
        var employee = CreateEmployee("C019-COLS");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom019ReportToExcel(Input(day, day, "C019-COLS"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        // Sanity first, so the ShouldNotContain assertions below cannot pass vacuously.
        headers.Count.ShouldBe(27);
        headers.ShouldContain("Code");
        headers.ShouldContain("Employee");
        headers.ShouldContain("Org Level 2");

        // The captions legacy commented out. These are the RESOLVED texts from HCMv3.xml, so a rename
        // there would fail this test rather than silently weaken it.
        foreach (var dropped in new[]
                 {
                     "Access Id", "Org Level 1", "Break Hours", "Country",
                     "Punch In Location", "Punch Out Location"
                 })
        {
            headers.ShouldNotContain(dropped);
        }
    }

    /// <summary>
    /// The FlexiTime branch uses the duty's real FlexiEndTime. The base control's collapsed-window defect
    /// -- where the end of the window was computed from the flexi START time -- is NOT present in this
    /// variant, so a flexi employee's punches are actually selected.
    /// </summary>
    [Fact]
    public async Task Should_Select_Flexi_Punches_Using_The_Real_Flexi_End_Time()
    {
        var employee = CreateEmployee("C019-FLEXI");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            flexiStartTime: TimeSpan.FromHours(6),
            flexiEndTime: TimeSpan.FromHours(20));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(
            employee,
            CreateTimePolicy(TimeEventRule.FirstInLastOut, TimeMgmtStatus.FlexiTime),
            new DateTime(2024, 6, 1),
            new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(7), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(19), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom019Report(Input(day, day, "C019-FLEXI"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(7));
        row.SignOut.ShouldBe(TimeSpan.FromHours(19));
    }
}
