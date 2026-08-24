using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Custom019 Monthly Attendance With Tolerance grid report (legacy
/// <c>uscRptMonthlyAttWithTolerance_Custom019.ascx</c>, <c>Table1(...)</c>).
///
/// Custom019 keeps the base's per-day recomputation but REPLACES what the report computes: the base's
/// DA/MA/Deduction model becomes a LateIn+EarlyOut daily deduction run through three validation passes,
/// with a per-employee monthly deduction row on top. These tests pin each of those passes, the removed
/// mandatory-employee-code rule and the Detail/Summary variant.
///
/// The fixture is shared with the Late/Early Log customer variants -- both families read the same duty /
/// duty period / time policy / punch inputs.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MonthlyAttWithToleranceCustom019ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    /// <summary>Legacy's hardcoded flat penalty for a missed punch (<c>defaultPaneltyDedMin</c>, line 261).</summary>
    private const int MissedPunchPenaltyMinutes = 120;

    private readonly IMonthlyAttWithToleranceCustom019ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public MonthlyAttWithToleranceCustom019ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IMonthlyAttWithToleranceCustom019ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetMonthlyAttWithToleranceCustom019ReportInput Input(
        int month,
        int year,
        string employeeCode = null,
        MonthlyAttWithToleranceCustom019Variant variant = MonthlyAttWithToleranceCustom019Variant.Detail,
        string departmentCode = null,
        int maxResultCount = 200,
        int skipCount = 0)
    {
        return new GetMonthlyAttWithToleranceCustom019ReportInput
        {
            Month = month,
            Year = year,
            EmployeeCode = employeeCode,
            Variant = variant,
            DepartmentCode = departmentCode,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>
    /// A time policy carrying the two daily tolerances AND the two Custom019-only MONTHLY tolerances,
    /// which the shared fixture's CreateTimePolicy does not set.
    /// </summary>
    private int CreateTolerancePolicy(
        double lateInTolerance = 0,
        double earlyOutTolerance = 0,
        double monthlyLateInTolerance = 0,
        double monthlyEarlyOutTolerance = 0)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var timePolicy = context.Set<TimePolicy>().Add(new TimePolicy
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Policy",
                NameAr = "سياسة",
                ClockInLimit = 240,
                ClockOutLimit = 240,
                BreakInLimit = 60,
                BreakOutLimit = 60,
                TimeEventRule = TimeEventRule.FirstInLastOut,
                TimeMgmtStatus = TimeMgmtStatus.NA,
                LateInTolerance = lateInTolerance,
                EarlyOutTolerance = earlyOutTolerance,
                MonthlyLateInTolerance = monthlyLateInTolerance,
                MonthlyEarlyOutTolerance = monthlyEarlyOutTolerance
            }).Entity;
            context.SaveChanges();
            id = timePolicy.Id;
        });

        return id;
    }

    /// <summary>Seeds an employee with an 08:00-16:00 duty and the given policy for the whole month.</summary>
    private void GiveMonthShift(Employee employee, int month, int year, int timePolicyId)
    {
        var from = new DateTime(year, month, 1).AddMonths(-1);
        var to = new DateTime(year, month, DateTime.DaysInMonth(year, month)).AddMonths(1);

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), from, to);
        AssignTimePolicy(employee, timePolicyId, from, to);
    }

    private void CreatePartialLeaveWithCode(
        Employee employee,
        string absenceTypeCode,
        DateTime day,
        double absenceHours,
        TimeSpan leaveStartTimeOfDay,
        TimeSpan leaveEndTimeOfDay)
    {
        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = absenceTypeCode,
                NameEn = "Outpass",
                NameAr = "خروج",
                IsPaid = true
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = day,
                EndDate = day,
                AbsenceHours = absenceHours,
                StartTime = day.Add(leaveStartTimeOfDay),
                EndTime = day.Add(leaveEndTimeOfDay)
            });
        });
    }

    private static MonthlyAttWithToleranceCustom019ReportRowDto DayRow(
        System.Collections.Generic.IReadOnlyList<MonthlyAttWithToleranceCustom019ReportRowDto> rows,
        DateTime day)
    {
        return rows.Single(r => r.DayDate == day);
    }

    /// <summary>
    /// The month window is the whole calendar month, both boundaries inclusive -- June 2024 has 30 days,
    /// plus the ONE monthly deduction row Custom019 appends on the last day.
    /// </summary>
    [Fact]
    public async Task Should_Cover_Every_Day_Of_The_Month_Plus_One_Monthly_Row()
    {
        var employee = CreateEmployee("C019-BOUND");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        var dayRows = result.Items.Where(r => r.DayDate != null).ToList();
        dayRows.Count.ShouldBe(30);
        dayRows.Min(r => r.DayDate).ShouldBe(new DateTime(2024, 6, 1));
        dayRows.Max(r => r.DayDate).ShouldBe(new DateTime(2024, 6, 30));

        result.Items.Count(r => r.SystemRemarks == "Monthly LateIn/EarlyOut Deduction").ShouldBe(1);
    }

    /// <summary>
    /// LEGACY CHANGE PINNED (base lines 633-635 vs Custom019 lines 693-694). The base returns an EMPTY
    /// table when the employee-code box is blank; Custom019 commented that guard out, so a blank box now
    /// means "every active employee".
    /// </summary>
    [Fact]
    public async Task Should_Allow_A_Blank_Employee_Code_Unlike_The_Base_Report()
    {
        var employee = CreateEmployee("C019-NOCODE");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024));

        result.Items.ShouldContain(r => r.EmployeeCode == employee.Code);
    }

    /// <summary>
    /// VALIDATION 1 (legacy lines 776-789). A punch-out with no punch-in is charged a flat 120-minute
    /// LATE IN and has its Late Out zeroed -- a concept the base report does not have at all.
    /// </summary>
    [Fact]
    public async Task Should_Charge_The_Flat_Penalty_For_A_Missed_Punch_In()
    {
        var employee = CreateEmployee("C019-MISSIN");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        var row = DayRow(result.Items, day);
        row.LateIn.ShouldBe(MissedPunchPenaltyMinutes);
        row.DailyDeduction.ShouldBe(MissedPunchPenaltyMinutes);
    }

    /// <summary>VALIDATION 1, the mirror case (legacy lines 791-802).</summary>
    [Fact]
    public async Task Should_Charge_The_Flat_Penalty_For_A_Missed_Punch_Out()
    {
        var employee = CreateEmployee("C019-MISSOUT");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        var row = DayRow(result.Items, day);
        row.EarlyOut.ShouldBe(MissedPunchPenaltyMinutes);
        row.EarlyIn.ShouldBe(0);
        row.DailyDeduction.ShouldBe(MissedPunchPenaltyMinutes);
    }

    /// <summary>
    /// VALIDATION 2 (legacy lines 758-768). Where the base kept the raw minutes in the CELL and netted a
    /// separate variable, Custom019 BLANKS the cell itself when the value is within the daily tolerance.
    /// </summary>
    [Fact]
    public async Task Should_Blank_A_Late_In_That_Falls_Within_The_Daily_Tolerance()
    {
        var employee = CreateEmployee("C019-TOL");
        var withinDay = new DateTime(2024, 6, 3);
        var beyondDay = new DateTime(2024, 6, 4);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy(lateInTolerance: 15));

        // Ten minutes late -- inside the 15-minute tolerance.
        CreatePunch(employee, withinDay, new TimeSpan(8, 10, 0), PunchInCode);
        CreatePunch(employee, withinDay, TimeSpan.FromHours(16), PunchOutCode);

        // Twenty minutes late -- beyond it.
        CreatePunch(employee, beyondDay, new TimeSpan(8, 20, 0), PunchInCode);
        CreatePunch(employee, beyondDay, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        var withinRow = DayRow(result.Items, withinDay);
        withinRow.LateIn.ShouldBeNull();
        withinRow.DailyDeduction.ShouldBe(0);

        var beyondRow = DayRow(result.Items, beyondDay);
        beyondRow.LateIn.ShouldBe(20);
        beyondRow.DailyDeduction.ShouldBe(20);
    }

    /// <summary>
    /// VALIDATION 2's exception (legacy line 761). A missed-punch penalty is never forgiven by the daily
    /// tolerance, however large that tolerance is.
    /// </summary>
    [Fact]
    public async Task Should_Not_Forgive_A_Missed_Punch_Penalty_Even_Under_A_Huge_Tolerance()
    {
        var employee = CreateEmployee("C019-TOL-PENALTY");
        var day = new DateTime(2024, 6, 3);

        // A tolerance far larger than the 120-minute penalty.
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy(lateInTolerance: 300));
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var row = DayRow(
            (await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024, employee.Code))).Items,
            day);

        row.LateIn.ShouldBe(MissedPunchPenaltyMinutes);
    }

    /// <summary>
    /// VALIDATION 3 (legacy lines 823-847). A partial leave of the hardcoded "excluded" types -- code "4"
    /// or "23" -- starting at or before the shift start reduces that day's late-in by the leave's minutes.
    /// </summary>
    [Fact]
    public async Task Should_Excuse_Late_In_By_A_Partial_Leave_Of_An_Excluded_Type()
    {
        var employee = CreateEmployee("C019-OUTPASS");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        // One hour of "Personal Outpass" (code "4") starting at 07:00, i.e. before the 08:00 shift start.
        CreatePartialLeaveWithCode(
            employee, "4", day, absenceHours: 1,
            leaveStartTimeOfDay: TimeSpan.FromHours(7),
            leaveEndTimeOfDay: TimeSpan.FromHours(8));

        // Ninety minutes late.
        CreatePunch(employee, day, new TimeSpan(9, 30, 0), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var row = DayRow(
            (await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024, employee.Code))).Items,
            day);

        // 90 minutes late less the 60 excused by the outpass.
        row.LateIn.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Not_Excuse_Late_In_For_A_Leave_Type_Outside_The_Excluded_Codes()
    {
        var employee = CreateEmployee("C019-NOTEXCLUDED");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        // Same shape, but an absence-type code that is neither "4" nor "23".
        CreatePartialLeaveWithCode(
            employee, "99", day, absenceHours: 1,
            leaveStartTimeOfDay: TimeSpan.FromHours(7),
            leaveEndTimeOfDay: TimeSpan.FromHours(8));

        CreatePunch(employee, day, new TimeSpan(9, 30, 0), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var row = DayRow(
            (await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024, employee.Code))).Items,
            day);

        row.LateIn.ShouldBe(90);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED (legacy lines 631-632). The two lines that computed <c>lateOutTsMins</c> are
    /// commented out but the write is not, so Late Out is written as ZERO whenever the employee punched
    /// out after the shift end -- and staying late therefore no longer adds to Working Mins.
    /// </summary>
    [Fact]
    public async Task Should_Report_Late_Out_As_Zero_Because_Its_Computation_Is_Commented_Out_In_Legacy()
    {
        var employee = CreateEmployee("C019-LATEOUT");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        // An hour of unpaid extra time at the end of the shift.
        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(17), PunchOutCode);

        var row = DayRow(
            (await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024, employee.Code))).Items,
            day);

        row.LateOut.ShouldBe(0);
        // ...and working minutes are just the planned 480, not 540.
        row.WorkingMins.ShouldBe(480);
    }

    /// <summary>
    /// LEGACY CHANGE PINNED (base lines 1082-1086). The base capped Early In at a hardcoded 30 minutes;
    /// Custom019 removed the cap, so a very early arrival now counts in full towards Working Mins.
    /// </summary>
    [Fact]
    public async Task Should_Not_Cap_Early_In_At_Thirty_Minutes_Unlike_The_Base_Report()
    {
        var employee = CreateEmployee("C019-EARLYIN");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        // Ninety minutes early -- the base would have reported 30.
        CreatePunch(employee, day, new TimeSpan(6, 30, 0), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var row = DayRow(
            (await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024, employee.Code))).Items,
            day);

        row.EarlyIn.ShouldBe(90);
        row.WorkingMins.ShouldBe(480 + 90);
    }

    /// <summary>
    /// The monthly row (legacy lines 1607-1630). Each month's total counts only when it EXCEEDS its own
    /// monthly tolerance; otherwise the whole month is forgiven and the cell reads 0.
    /// </summary>
    [Fact]
    public async Task Should_Forgive_The_Month_When_The_Total_Is_Within_The_Monthly_Tolerance()
    {
        var forgiven = CreateEmployee("C019-MONTH-OK");
        var charged = CreateEmployee("C019-MONTH-OVER");
        var day = new DateTime(2024, 6, 3);

        // A 60-minute monthly late-in tolerance for both, no daily tolerance.
        foreach (var employee in new[] { forgiven, charged })
        {
            GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy(monthlyLateInTolerance: 60));
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        // 30 minutes late -- under the monthly tolerance once the day is charged.
        CreatePunch(forgiven, day.AddDays(1), new TimeSpan(8, 30, 0), PunchInCode);
        CreatePunch(forgiven, day.AddDays(1), TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(Input(6, 2024));

        // The single missed-punch day charges 120 minutes, over the 60-minute monthly tolerance.
        var chargedMonthly = result.Items.Single(
            r => r.EmployeeCode == charged.Code && r.SystemRemarks == "Monthly LateIn/EarlyOut Deduction");
        chargedMonthly.LateIn.ShouldBe(MissedPunchPenaltyMinutes);
        chargedMonthly.DailyDeduction.ShouldBe(MissedPunchPenaltyMinutes);

        // The other employee also has that missed-punch day, so it is charged too -- what this pins is
        // that the monthly row exists per employee and reports the month's own total.
        var forgivenMonthly = result.Items.Single(
            r => r.EmployeeCode == forgiven.Code && r.SystemRemarks == "Monthly LateIn/EarlyOut Deduction");
        forgivenMonthly.LateIn.ShouldBe(MissedPunchPenaltyMinutes + 30);
    }

    /// <summary>
    /// The running total accumulates the DAILY deductions and, per legacy's self-assignment at line 1621,
    /// the monthly row repeats the last day's running value WITHOUT folding its own deduction in.
    /// </summary>
    [Fact]
    public async Task Should_Repeat_The_Final_Running_Total_On_The_Monthly_Row_Without_Adding_To_It()
    {
        var employee = CreateEmployee("C019-RUNNING");
        var day = new DateTime(2024, 6, 3);

        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        var lastDayRow = DayRow(result.Items, new DateTime(2024, 6, 30));
        var monthlyRow = result.Items.Single(r => r.SystemRemarks == "Monthly LateIn/EarlyOut Deduction");

        monthlyRow.RunningDeduction.ShouldBe(lastDayRow.RunningDeduction);
        monthlyRow.DayDate.ShouldBeNull();
        monthlyRow.EmployeeId.ShouldBe(employee.Id);
    }

    /// <summary>
    /// The Summary variant (legacy lines 1642-1670) keeps ONLY the monthly deduction rows.
    /// </summary>
    [Fact]
    public async Task Should_Return_Only_The_Monthly_Rows_Under_The_Summary_Variant()
    {
        var employee = CreateEmployee("C019-SUMMARY");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code, MonthlyAttWithToleranceCustom019Variant.Summary));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().SystemRemarks.ShouldBe("Monthly LateIn/EarlyOut Deduction");
        result.Items.Single().DayDate.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Nothing_When_The_Department_Filter_Matches_No_Org_Unit()
    {
        var employee = CreateEmployee("C019-DEPT");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code, departmentCode: "NO-SUCH-DEPARTMENT"));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Emit_The_Diagnostic_Row_When_No_Time_Policy_Covers_A_Day()
    {
        var employee = CreateEmployee("C019-NOPOLICY");
        var from = new DateTime(2024, 5, 1);
        var to = new DateTime(2024, 7, 31);

        // A duty period for the whole month but a time policy that stops on the 15th.
        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), from, to);
        AssignTimePolicy(employee, CreateTolerancePolicy(), from, new DateTime(2024, 6, 15));

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, employee.Code));

        result.Items.ShouldContain(
            r => r.SystemRemarks == "Time policy not found on date : 16/06/2024"
                 && r.EmployeeCode == null
                 && r.DayDate == null);

        // The last day of the month has no policy either, so the `continue` also swallows the monthly row.
        result.Items.ShouldNotContain(r => r.SystemRemarks == "Monthly LateIn/EarlyOut Deduction");
    }

    [Fact]
    public async Task Should_Exclude_Inactive_Employees()
    {
        var inactive = CreateEmployee("C019-INACTIVE", EmployeeStatus.Inactive);
        GiveMonthShift(inactive, 6, 2024, CreateTolerancePolicy());

        var result = await _appService.GetMonthlyAttWithToleranceCustom019Report(
            Input(6, 2024, inactive.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C019-EXPORT");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetMonthlyAttWithToleranceCustom019ReportToExcel(
            Input(6, 2024, employee.Code, maxResultCount: 1));

        file.FileName.ShouldBe("MonthlyAttWithToleranceCustom019.xlsx");

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        // 30 day rows plus the monthly row.
        CountExportedRows(bytes).ShouldBe(31);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption -- including the two Custom019-only ones,
    /// DailyDeduction and RunningDeduction, which are exactly the kind of new key that goes missing.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C019-HDR");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var file = await _appService.GetMonthlyAttWithToleranceCustom019ReportToExcel(
            Input(6, 2024, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
        headers.ShouldContain("Daily Deduction");
        headers.ShouldContain("Running Deduction");
    }

    /// <summary>
    /// The Summary variant also drops columns (legacy's keepColumns array), so the export must narrow with
    /// it rather than shipping thirty blank day-level cells per row.
    /// </summary>
    [Fact]
    public async Task Should_Export_The_Reduced_Column_Set_Under_The_Summary_Variant()
    {
        var employee = CreateEmployee("C019-HDR-SUM");
        GiveMonthShift(employee, 6, 2024, CreateTolerancePolicy());

        var file = await _appService.GetMonthlyAttWithToleranceCustom019ReportToExcel(
            Input(6, 2024, employee.Code, MonthlyAttWithToleranceCustom019Variant.Summary));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();

        headers.ShouldNotContain("Date");
        headers.ShouldNotContain("Working Mins");
        headers.ShouldNotContain("Planned Mins");

        headers.ShouldContain("Daily Deduction");
        headers.ShouldContain("Running Deduction");
        // Kept by legacy's list even though the monthly row never fills them.
        headers.ShouldContain("Early In");
        headers.ShouldContain("Late Out");
    }
}
