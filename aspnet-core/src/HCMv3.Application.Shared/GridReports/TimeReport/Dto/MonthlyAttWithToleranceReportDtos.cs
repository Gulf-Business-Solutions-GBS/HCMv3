using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Monthly Attendance With Tolerance grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance.ascx</c>).
///
/// ATYPICAL FILTER PANEL (designer lines 69-96): <c>txtMonth</c>, <c>txtYear</c>, <c>cmbPayrollArea</c>
/// and <c>txtEmpCode</c>. There is no date range and no employee-status combo, so the inherited
/// <c>FromDate</c>/<c>ToDate</c>/<c>EmployeeStatus</c> are unused and the Angular screen hides them --
/// the reporting window is the whole calendar month (lines 480-494).
///
/// <c>cmbPayrollArea</c> IS DEAD IN LEGACY: the block that would have applied it is commented out
/// (lines 651-659), so the combo has no effect on the data. It is therefore NOT carried here; adding a
/// working payroll-area filter would change what the screen returns.
///
/// EMPLOYEE CODE IS MANDATORY: legacy returns an EMPTY table immediately when the box is blank
/// (lines 633-635), because the report recomputes attendance day by day and is far too expensive to run
/// for the whole company. Reproduced -- see <see cref="EmployeeCode"/>.
/// </summary>
public class GetMonthlyAttWithToleranceReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>txtMonth</c>. Null means the CURRENT month (legacy defaulted a blank box to
    /// <c>DateTime.Now.Month</c>, line 480).
    /// </summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>
    /// Legacy <c>txtYear</c>. Null means the CURRENT year (legacy defaulted a blank box to
    /// <c>DateTime.Now.Year</c>, line 482).
    /// </summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy generated rows employee by employee and day by day, so a stable order is both the legacy
    /// reading order and what server-side paging needs.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DayDate ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptMonthlyAttWithTolerance.ascx.cs:544-1268).
///
/// GRAIN: one row per (employee, calendar day of the selected month) -- NOT one per stored record. This
/// report does not read <c>TimeRegister</c> at all; it RECOMPUTES the day from raw
/// <c>TimeEvent</c> punches against the employee's duty period, time policy, substitute duties, public
/// holidays and absences, then accumulates the running monthly deduction against the policy's
/// <c>MonthlyAfterTolerance</c>. That is why it is the most expensive report in the family and why an
/// employee code is mandatory.
///
/// Legacy column set, in DataTable order (lines 605-628): <c>Oid</c>, <c>Code</c>, <c>Employee</c>,
/// <c>Date</c>, <c>WeekDay</c>, <c>Shift Start</c>, <c>Shift End</c>, <c>Sign In</c>, <c>Sign Out</c>,
/// <c>Leave Mins</c>, <c>Early In</c>, <c>Late In</c>, <c>Early Out</c>, <c>Late Out</c>,
/// <c>Working Mins</c>, <c>Planned Mins</c>, <c>DA</c>, <c>MA</c>, <c>Deduction</c>,
/// <c>System Remarks</c>. (<c>Oid</c> is never assigned by legacy on this screen -- it is declared and
/// left empty -- so it has no counterpart here.)
///
/// NULL IS MEANINGFUL: legacy leaves a cell as <c>DBNull</c> rather than writing 0 in many branches (an
/// unset Early In is blank, not "0"), so the numeric columns are nullable here and the exporter writes
/// blanks where legacy wrote blanks.
/// </summary>
public class MonthlyAttWithToleranceReportRowDto
{
    /// <summary>
    /// Legacy "Code" column. NULL on the two diagnostic rows -- see <see cref="SystemRemarks"/>.
    /// </summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "Employee" column -- <c>FullNameAr</c> in Arabic else <c>FullNameEn</c> (line 660).
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "Date" column. NULL on the two diagnostic rows.</summary>
    public DateTime? DayDate { get; set; }

    /// <summary>
    /// Legacy "WeekDay" column -- the day name, LOCALIZED by legacy through a hand-written
    /// English/Arabic ternary chain (lines 838-845) rather than through the resource files.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>Legacy "Shift Start" column -- the duty's start time as short time text.</summary>
    public string ShiftStart { get; set; }

    /// <summary>Legacy "Shift End" column.</summary>
    public string ShiftEnd { get; set; }

    /// <summary>Legacy "Sign In" column -- the resolved punch-in time, blank when there was none.</summary>
    public string SignIn { get; set; }

    /// <summary>Legacy "Sign Out" column.</summary>
    public string SignOut { get; set; }

    /// <summary>
    /// Legacy "Leave Mins" column -- the total partial-leave minutes on the day, written ONLY when
    /// positive (line 1195).
    /// </summary>
    public int? LeaveMins { get; set; }

    /// <summary>
    /// Legacy "Early In" column -- minutes punched in before the shift start, CAPPED AT 30 (lines
    /// 1082-1086: <c>earlyInMaxAllowedlimit</c> is a hardcoded local, not a policy setting). Written only
    /// when the employee actually came early.
    /// </summary>
    public int? EarlyIn { get; set; }

    /// <summary>
    /// Legacy "Late In" column -- minutes punched in after the shift start, BEFORE the policy's
    /// <c>LateInTolerance</c> is applied (line 1101 writes the raw <c>lateInTsMins</c>; the tolerance-netted
    /// value feeds the deduction, not this cell). Written only when the employee was late.
    /// </summary>
    public int? LateIn { get; set; }

    /// <summary>
    /// Legacy "Early Out" column -- raw minutes left before the shift end, again before the
    /// <c>EarlyOutTolerance</c> netting. Written only when the employee left early.
    /// </summary>
    public int? EarlyOut { get; set; }

    /// <summary>
    /// Legacy "Late Out" column -- minutes stayed past the shift end. LEGACY QUIRK, REPRODUCED (line
    /// 1134): if the employee was late in by more than the late-in tolerance, late-out is forced to 0.
    /// </summary>
    public int? LateOut { get; set; }

    /// <summary>
    /// Legacy "Working Mins" column -- <c>PlannedMins + EarlyIn + LateOut + paidPartialLeave -
    /// nettedLateIn - nettedEarlyOut</c>, and only when BOTH punches exist (lines 1209-1221). Never
    /// written on a public holiday, and never written when it computes to 0 (lines 1223-1229).
    /// </summary>
    public int? WorkingMins { get; set; }

    /// <summary>
    /// Legacy "Planned Mins" column -- the shift length, zero on a week-off. Never written on a public
    /// holiday, and never written when it computes to 0.
    /// </summary>
    public int? PlannedMins { get; set; }

    /// <summary>
    /// Legacy "DA" (daily adjustment) column -- <c>PlannedMins - WorkingMins</c>, forced to 0 when the day
    /// carries an unpaid leave or is a public holiday (lines 1232-1237). Not written at all on a full-day
    /// leave row.
    /// </summary>
    public int? DA { get; set; }

    /// <summary>
    /// Legacy "MA" (monthly accumulation) column -- the running sum of the POSITIVE DA values from the
    /// first of the month to this day (lines 1239-1243); negative DA is ignored so extra time does not
    /// erase earlier deductions. Not written on a full-day leave row.
    /// </summary>
    public int? MA { get; set; }

    /// <summary>
    /// Legacy "Deduction" column, a BOOLEAN -- <c>MA &gt; TimePolicy.MonthlyAfterTolerance</c>, i.e. "the
    /// month has now exceeded the tolerance" (line 1245). This is the column the report is named after.
    /// </summary>
    public bool? Deduction { get; set; }

    /// <summary>
    /// Legacy "System Remarks" column -- the <c>GetStatus(...)</c> verdict (lines 1285-1338): the public
    /// holiday name, "WeekEnd", the leave type, "First/Second Half Day Leave", "Late", "Early",
    /// "Late + Early", "Extra time", "Missed In", "Missed Out", "No Show" or "".
    ///
    /// It is also the ONLY populated cell on the two DIAGNOSTIC rows legacy emits when the employee has no
    /// time policy or no duty period covering a day: "Time policy not found on date : dd/MM/yyyy" and
    /// "Duty Period not found on date : dd/MM/yyyy" (lines 861-880). Those rows carry no code, no name and
    /// no date -- reproduced verbatim, and pinned by a test.
    ///
    /// LEGACY FIDELITY: all of these verdicts are HARDCODED ENGLISH literals in the legacy source, never
    /// localized, so they are emitted as-is here.
    /// </summary>
    public string SystemRemarks { get; set; }
}
