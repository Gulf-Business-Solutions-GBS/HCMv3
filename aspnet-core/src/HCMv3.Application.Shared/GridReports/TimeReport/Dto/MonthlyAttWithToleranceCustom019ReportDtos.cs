using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Legacy <c>cmbVarient</c> (populated at uscRptMonthlyAttWithTolerance_Custom019.ascx.cs:320-325 with
/// exactly two items, "Detail" first and selected by default).
/// </summary>
public enum MonthlyAttWithToleranceCustom019Variant
{
    /// <summary>Every generated row: the per-day rows plus the per-employee monthly deduction row.</summary>
    Detail = 0,

    /// <summary>
    /// ONLY the per-employee monthly deduction rows (legacy lines 1642-1670 filter the DataTable down to
    /// <c>Status == "Monthly LateIn/EarlyOut Deduction"</c> and then DROP every column except the ten
    /// listed in <c>keepColumns</c>).
    /// </summary>
    Summary = 1
}

/// <summary>
/// Filter input for the Custom019 Monthly Attendance With Tolerance grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance_Custom019.ascx</c>).
///
/// A WIDER PANEL than the base's. Legacy's <c>GetParameterStr()</c> (lines 645-670) enumerates it exactly:
/// Month, Year, Payroll Area, Varient, Department, Division, Section and Employee Code. Against the base
/// input this adds <see cref="Variant"/>, <see cref="DepartmentCode"/>, <see cref="DivisionCode"/> and
/// <see cref="SectionCode"/>.
///
/// EMPLOYEE CODE IS NO LONGER MANDATORY. The base returns an empty table on a blank code box (base
/// lines 633-635); Custom019 COMMENTED THAT OUT (lines 693-694) and replaced it with a guard on the month
/// window instead, because the three org filters give the customer another way to bound the run. The
/// report is still a per-day recomputation, so a completely unfiltered run is expensive -- but that is
/// what the screen does.
///
/// <c>cmbPayrollArea</c> IS STILL DEAD: it is on the panel and in the parameter string, but the code that
/// would apply it is absent (the base's already-commented block was deleted outright). Not carried --
/// implementing it would change what the screen returns.
/// </summary>
public class GetMonthlyAttWithToleranceCustom019ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Null means the CURRENT month.</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Null means the CURRENT year.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbDepartment</c>, matched against <c>OrgLevel2.Code</c> on an org assignment covering
    /// TODAY -- not the reported month (line 711).
    /// </summary>
    public string DepartmentCode { get; set; }

    /// <summary>
    /// Legacy <c>cmbDivision</c>, matched against <c>OrgLevel3.Code</c> on an org assignment covering
    /// TODAY (line 720).
    /// </summary>
    public string DivisionCode { get; set; }

    /// <summary>
    /// Legacy <c>cmbSection</c>, matched against <c>OrgLevel4.Code</c> on an org assignment covering
    /// TODAY (line 729).
    /// </summary>
    public string SectionCode { get; set; }

    /// <summary>Legacy <c>cmbVarient</c>. Defaults to Detail, as the combo did.</summary>
    public MonthlyAttWithToleranceCustom019Variant Variant { get; set; }
        = MonthlyAttWithToleranceCustom019Variant.Detail;

    /// <summary>
    /// Legacy generated rows employee by employee and day by day, then appended the monthly row last, so
    /// a stable order is both the legacy reading order and what server-side paging needs. Note the monthly
    /// row has no date, so it sorts FIRST within an employee under an ascending date sort -- see
    /// <see cref="MonthlyAttWithToleranceCustom019ReportRowDto.DayDate"/>.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DayDate ASC";
}

/// <summary>
/// One grid row of legacy <c>uscRptMonthlyAttWithTolerance_Custom019.ascx.cs</c> <c>Table1(...)</c>
/// (lines 675-1740).
///
/// SAME GRAIN AS THE BASE plus one extra row shape: one row per (employee, calendar day of the month),
/// the two diagnostic rows, and -- new here -- ONE MONTHLY DEDUCTION ROW PER EMPLOYEE emitted on the last
/// day of the month (lines 1607-1630), identified by
/// <c>SystemRemarks == "Monthly LateIn/EarlyOut Deduction"</c>.
///
/// THE COLUMN SET CHANGED. Against
/// <see cref="MonthlyAttWithToleranceReportRowDto"/> Custom019:
/// <list type="bullet">
/// <item><description>RENAMES <c>DA</c> to <c>DailyDeduction</c> and <c>MA</c> to <c>RunningDeduction</c>
/// -- and they are no longer the same numbers, see <see cref="DailyDeduction"/>.</description></item>
/// <item><description>DROPS the boolean <c>Deduction</c> column entirely (the write is commented out,
/// line 465). <c>TimePolicy.MonthlyAfterTolerance</c> is still read into a local and then never
/// used.</description></item>
/// <item><description>RENAMES the <c>System Remarks</c> caption to <c>Status</c> and ADDS <c>Oid</c>,
/// which -- unlike the base, where it was declared and never filled -- now carries the EMPLOYEE id
/// (lines 417/483/1609).</description></item>
/// </list>
/// </summary>
public class MonthlyAttWithToleranceCustom019ReportRowDto
{
    /// <summary>
    /// Legacy "Oid" column, which on this variant is the EMPLOYEE's id, not a row id. NULL on the two
    /// diagnostic rows, which legacy leaves entirely blank apart from the status cell.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>Legacy "Code" column. NULL on the two diagnostic rows.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- <c>FullNameAr</c> in Arabic else <c>FullNameEn</c>.</summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Date" column. NULL on the two diagnostic rows AND on the monthly deduction row, which
    /// legacy fills with only the employee, the two totals and the status.
    /// </summary>
    public DateTime? DayDate { get; set; }

    /// <summary>Legacy "WeekDay" column -- localized through a hand-written ternary chain, not resources.</summary>
    public string WeekDay { get; set; }

    public string ShiftStart { get; set; }

    public string ShiftEnd { get; set; }

    public string SignIn { get; set; }

    public string SignOut { get; set; }

    /// <summary>Legacy "Leave Mins" column -- total partial-leave minutes, written only when positive.</summary>
    public int? LeaveMins { get; set; }

    /// <summary>
    /// Legacy "Early In" column. CUSTOM019 REMOVED THE BASE'S 30-MINUTE CAP (base lines 1082-1086 have
    /// <c>earlyInMaxAllowedlimit</c>; Custom019 line 579 writes the raw minutes), so a very early arrival
    /// now counts in full towards Working Mins. It is FORCED TO ZERO on a missed-punch-out day
    /// (line 728).
    /// </summary>
    public int? EarlyIn { get; set; }

    /// <summary>
    /// Legacy "Late In" column, and on the monthly row the month's accumulated late-in deduction. Unlike
    /// the base this cell is MUTATED by the later validation passes: it is blanked when it falls within
    /// the daily tolerance (line 762), forced to the 120-minute penalty on a missed punch-in (line 717),
    /// and reduced by a "Personal Outpass"/"Business Meeting" partial leave (line 833).
    /// </summary>
    public int? LateIn { get; set; }

    /// <summary>
    /// Legacy "Early Out" column, and on the monthly row the month's accumulated early-out deduction.
    /// Mutated by the same three passes as <see cref="LateIn"/>.
    /// </summary>
    public int? EarlyOut { get; set; }

    /// <summary>
    /// Legacy "Late Out" column. EFFECTIVELY DEAD IN CUSTOM019: the two lines that computed it are
    /// commented out (lines 631-632) but the write is not, so the cell is written as ZERO whenever the
    /// employee punched out after the shift end and left blank otherwise. Staying late therefore no
    /// longer feeds Working Mins. Reproduced and pinned by a test.
    /// </summary>
    public int? LateOut { get; set; }

    /// <summary>
    /// Legacy "Working Mins" column. Custom019 computes it whenever the day is NOT a week off
    /// (line 794) where the base required BOTH punches to exist, and it uses the RAW late-in/early-out
    /// minutes where the base used the tolerance-netted ones.
    /// </summary>
    public int? WorkingMins { get; set; }

    /// <summary>Legacy "Planned Mins" column -- the shift length, zero on a week-off.</summary>
    public int? PlannedMins { get; set; }

    /// <summary>
    /// Legacy "DailyDeduction" column, which REPLACES the base's DA and is a different quantity: the base's
    /// DA was <c>PlannedMins - WorkingMins</c>, whereas this is <c>LateIn + EarlyOut</c> after all the
    /// validation passes (line 843), and 0 on a public holiday. On the monthly row it is the sum of the two
    /// monthly totals.
    /// </summary>
    public int? DailyDeduction { get; set; }

    /// <summary>
    /// Legacy "RunningDeduction" column, which REPLACES the base's MA: the running sum of
    /// <see cref="DailyDeduction"/> from the first of the month. Unlike the base's MA it accumulates every
    /// daily value, negative or not -- though <see cref="DailyDeduction"/> can no longer be negative. The
    /// monthly row repeats the final running total without adding its own deduction to it (line 1621, a
    /// self-assignment in legacy).
    /// </summary>
    public int? RunningDeduction { get; set; }

    /// <summary>
    /// Legacy "Status" column (the base called the same column "System Remarks") -- the
    /// <c>GetStatus(...)</c> verdict (lines 1750-1810), the two diagnostic messages, or the literal
    /// <c>"Monthly LateIn/EarlyOut Deduction"</c> on the monthly row.
    ///
    /// GetStatus itself changed from the base: a public holiday now reads <c>"{name} (PH)"</c>, a leave
    /// type reads <c>"{name} (Leave)"</c>, the "Extra time" verdict is gone (it depended on the removed DA)
    /// and a day with neither punch reads <c>"Missed In + Missed Out"</c> where the base said
    /// <c>"No Show"</c>. All verdicts remain HARDCODED ENGLISH literals in legacy and are emitted as-is.
    ///
    /// Note the ORDER also changed: Custom019 computes the status BEFORE the missed-punch and tolerance
    /// validation passes (line 771), so it reflects the RAW late-in/early-out, not the adjusted values the
    /// LateIn/EarlyOut cells end up showing.
    /// </summary>
    public string SystemRemarks { get; set; }
}
