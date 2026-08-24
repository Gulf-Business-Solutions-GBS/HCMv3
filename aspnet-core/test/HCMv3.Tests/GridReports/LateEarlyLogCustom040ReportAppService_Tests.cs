using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Custom040 Late/Early Log grid report (legacy
/// <c>uscRptLateEarlyLog_Custom040.ascx</c>, <c>Table1()</c>).
///
/// Custom040 is an OLDER, BREAK-LESS fork of the base control, so the tests concentrate on the four places
/// where it disagrees with the already-ported base: the status vocabulary, the missing break columns, the
/// time-of-day (rather than date-time) early/late arithmetic and the break-less planned-hours formula.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom040ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    private readonly ILateEarlyLogCustom040ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom040ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom040ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom040ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetLateEarlyLogCustom040ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Include_Both_Boundary_Days_Of_The_Window()
    {
        var employee = CreateEmployee("C040-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom040Report(
            Input(from, to, employee.Code));

        // One row per calendar day, both boundaries inclusive.
        result.Items.Select(r => r.Date).OrderBy(d => d)
            .ShouldBe(new[] { from, from.AddDays(1), to });
    }

    [Fact]
    public async Task Should_Return_Nothing_When_FromDate_Is_After_ToDate()
    {
        var employee = CreateEmployee("C040-INVERTED");
        GiveStandardDayShift(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetLateEarlyLogCustom040Report(
            Input(new DateTime(2024, 6, 12), new DateTime(2024, 6, 10), employee.Code));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// THE SIGNATURE CUSTOM040 CHANGE (legacy lines 678-690). The base reports "Missed In" / "Missed Out";
    /// this customer reports "Present with Single Punch" when exactly one of the two punch lists is
    /// non-empty and "No Punch Found" when both are.
    /// </summary>
    [Fact]
    public async Task Should_Report_The_Custom040_Status_Vocabulary_Instead_Of_Missed_In_Or_Missed_Out()
    {
        var employee = CreateEmployee("C040-STATUS");
        var day = new DateTime(2024, 6, 10);
        var noPunchDay = day.AddDays(1);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        // Only a clock-in on the first day; nothing at all on the second.
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom040Report(
            Input(day, noPunchDay, employee.Code));

        var singlePunchRow = result.Items.Single(r => r.Date == day);
        singlePunchRow.Status.ShouldBe("Present with Single Punch");

        var noPunchRow = result.Items.Single(r => r.Date == noPunchDay);
        noPunchRow.Status.ShouldBe("No Punch Found");

        // And never the base's wording.
        result.Items.ShouldAllBe(r => !r.Status.Contains("Missed"));
    }

    [Fact]
    public async Task Should_Report_An_Empty_Status_When_Both_Punches_Are_Present()
    {
        var employee = CreateEmployee("C040-CLEAN");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, employee.Code));

        result.Items.Single().Status.ShouldBe(string.Empty);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED (legacy lines 720-721). Planned hours are <c>shiftStart - shiftEnd</c> on a
    /// night shift, computed from BARE TIMES, so a 22:00-06:00 shift reports SIXTEEN hours instead of
    /// eight. The base report avoided this by anchoring both bounds to real dates; Custom040 predates that
    /// fix and the wave's standing rule is legacy fidelity.
    /// </summary>
    [Fact]
    public async Task Should_Report_Sixteen_Planned_Hours_For_An_Eight_Hour_Night_Shift_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C040-NIGHT");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(22), TimeSpan.FromHours(6), isNightShift: true);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, employee.Code));

        // 22:00 - 06:00 = 16 hours, not the eight hours actually worked.
        result.Items.Single().PlannedHours.ShouldBe(TimeSpan.FromHours(16));
    }

    /// <summary>
    /// LEGACY DEFECT PINNED (legacy lines 610/615). Early/late are measured on TIME-OF-DAY only, so a
    /// night-shift clock-in at 22:30 against a 22:00 shift start is 30 minutes late -- but a clock-out at
    /// 06:30 the NEXT morning is measured against 06:00 on the SAME calendar day, which happens to give the
    /// right answer, while the punch itself is selected from the correct cross-midnight window. The
    /// observable consequence tested here is that the day-shift arithmetic is plain time-of-day
    /// subtraction with no date component.
    /// </summary>
    [Fact]
    public async Task Should_Measure_Late_In_On_Time_Of_Day_Only()
    {
        var employee = CreateEmployee("C040-LATE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));
        CreatePunch(employee, day, new TimeSpan(8, 45, 0), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.LateIn.ShouldBe(TimeSpan.FromMinutes(45));
        row.EarlyIn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Report_A_Full_Day_Leave_Row_Charging_The_Whole_Planned_Shift_As_Absence()
    {
        var employee = CreateEmployee("C040-LEAVE");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));
        CreateAbsence(employee, CreateAbsenceType(), day, day);

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.Status.ShouldBe("Leave");
        // Unlike Custom045 and Custom053, Custom040 KEEPS the base's `absencehours = plannedHours`.
        row.AbsenceHours.ShouldBe(TimeSpan.FromHours(8));
        row.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
        row.TotalHours.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_Skip_An_Employee_With_No_Duty_Period_In_The_Window()
    {
        var employee = CreateEmployee("C040-NODP");
        var day = new DateTime(2024, 6, 10);

        // A time policy but NO duty period -- legacy's `dsCount > 0` guard drops the employee entirely.
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, employee.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var day = new DateTime(2024, 6, 10);

        foreach (var code in new[] { "C040-F-A", "C040-F-B", "C040-F-C" })
        {
            GiveStandardDayShift(CreateEmployee(code), day.AddDays(-30), day.AddDays(30));
        }

        var result = await _appService.GetLateEarlyLogCustom040Report(
            Input(day, day, "C040-F-A, C040-F-C"));

        result.Items.Select(r => r.EmployeeCode).OrderBy(c => c)
            .ShouldBe(new[] { "C040-F-A", "C040-F-C" });
    }

    [Fact]
    public async Task Should_Exclude_Inactive_Employees()
    {
        var day = new DateTime(2024, 6, 10);
        var inactive = CreateEmployee("C040-INACTIVE", EmployeeStatus.Inactive);
        GiveStandardDayShift(inactive, day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom040Report(Input(day, day, inactive.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C040-EXPORT");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 14);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetLateEarlyLogCustom040ReportToExcel(
            Input(from, to, employee.Code, maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom040.xlsx");

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        CountExportedRows(bytes).ShouldBe(5);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C040-HDR");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom040ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// Custom040 does not compute the break at all, so the three break columns the base exports must be
    /// absent from this variant's export -- dropping them silently would be the easy mistake.
    /// </summary>
    [Fact]
    public async Task Should_Not_Export_The_Break_Columns_The_Base_Report_Has()
    {
        var employee = CreateEmployee("C040-NOBREAK");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom040ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotContain("Break In");
        headers.ShouldNotContain("Break Out");
        headers.ShouldNotContain("Break Hours");

        // ...but the columns it DOES share with the base are still there.
        headers.ShouldContain("Early In");
        headers.ShouldContain("Late Out");
    }
}
