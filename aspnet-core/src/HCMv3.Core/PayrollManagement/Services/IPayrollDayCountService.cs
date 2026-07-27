using System;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Payroll "day counting" layer: the set of <c>Get*Days</c> calculators the legacy payroll engine ran once per
/// employee per salary register to produce the <b>TimeType</b> wage-type <c>Number</c> values (4014 Present
/// Days, 4011 Active Days, 4001/4002 Absent Days, 4007 Public Holiday Days, 4008 Weekly Off Days, 4009 Rest
/// Days, 4003 Sick Leave Paid, 4005 Sick Leave Unpaid, 4015 Paid Days).
///
/// <para>Ported from the legacy console project <c>HCM10.PayrollEngine/Program.cs</c>, region
/// <c>&lt;&lt; Time Types Balances Calculation &gt;&gt;</c>. The legacy <c>Main()</c>/<c>args[]</c>/XAF
/// <c>IObjectSpace</c> plumbing is deliberately NOT ported -- only the algorithms. Legacy's process-wide
/// <c>static</c> caches (<c>empTimeRegister</c>, <c>empTimeRegisterList</c>, <c>empContractList</c>,
/// <c>empDutyPeriodList</c>, <c>WageTypeList</c>, plus the ~15 <c>static bool</c> setting mirrors loaded once
/// in <c>GetSysSettings()</c>) are replaced by per-call repository queries and per-call
/// <see cref="HCMv3.SystemSettings.Services.ISysSettingsAccessor"/> reads. See
/// <see cref="PayrollDayCountService"/> for the full deviation list.</para>
///
/// <para><b>Legacy's two TimeRegister caches.</b> Legacy read some branches from <c>empTimeRegisterList</c>
/// (a <c>List</c> of the employee's TimeRegister rows for <c>payStartDate..payDate</c>, populated only on
/// Regular runs and explicitly set to <c>null</c> on OffCycle runs) and others from <c>empTimeRegister</c>
/// (an <c>IQueryable</c> over <c>payStartDate..payInDate</c> / <c>..offCycleEndDate</c>). This port uses a
/// single repository query per call and lets each method's own <c>DayDate &gt;= x &amp;&amp; DayDate &lt;= y</c>
/// predicate define the window. For SEVEN of the nine methods that predicate is genuinely a subset of both
/// caches' windows, so collapsing the caches is behaviour-preserving. <b>Two methods are exceptions and must
/// not be assumed safe:</b></para>
/// <list type="bullet">
/// <item><description><see cref="GetPresentDaysAsync"/> reads with a month-start..month-<i>END</i>
/// predicate, which on an off-cycle run ending mid-month runs PAST the cache's
/// <c>offCycleEndDate</c> upper bound. The cap is therefore re-applied explicitly in the implementation --
/// without it the fixed-days/actual-days switch sees a full month of rows and over-pays. Covered by
/// <c>PayrollDayCountServiceTests.GetPresentDaysAsync_Should_Switch_To_Actual_Working_Days_When_The_OffCycle_Leave_Ends_MidMonth</c>.</description></item>
/// <item><description><see cref="GetAbsentDaysAsync"/>, when its wage type is
/// <c>IncludeInCutoff</c>, shifts its window START back into the PREVIOUS month, below the cache's
/// <c>payStartDate</c> lower bound. <b>KNOWN, DELIBERATE DIVERGENCE:</b> legacy's Regular branch reads
/// <c>empTimeRegisterList</c> (bounded at <c>payStartDate</c>), so legacy silently drops the
/// previous-month portion of the cut-off range -- the cut-off feature never actually reaches backwards on a
/// Regular run. This port does NOT reproduce that clipping: it honours the full cut-off window, because the
/// clipping is far more likely to be a legacy defect than intent. This changes absence pay for cut-off wage
/// types and needs a payroll-parity sign-off before release; if parity with legacy is required, clamp
/// <c>ApplyCutoffSetting</c>'s start date up to <c>payStartDate</c> on the Regular path. Behaviour pinned by
/// <c>PayrollDayCountServiceTests.GetAbsentDaysAsync_Should_Shift_The_Window_To_The_Cutoff_Range_When_IncludeInCutoff</c>.</description></item>
/// </list>
///
/// <para><b>Relationship to <see cref="ITimeDataService"/> and <see cref="IAccrualService"/> -- read this
/// before "unifying" anything.</b> Several methods here look like near-duplicates of members on
/// <c>HCMv3.TimeManagement.Services.ITimeDataService</c> (<c>GetPublicHolidaysDatesAsync</c>,
/// <c>GetRestDaysCountAsync</c>, <c>GetRestDaysListAsync</c>, <c>GetWeeklyOffDaysCountAsync</c>) and on
/// <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>. They are NOT duplicates, and merging
/// them would be a payroll bug:</para>
/// <list type="bullet">
/// <item><description><b>ITimeDataService counts from MASTER data</b> -- it walks the calendar day by day and
/// resolves each day's <c>Duty</c>/<c>PublicHoliday</c>/roster configuration to decide what that day
/// <i>ought</i> to be. It is the input side of time evaluation and is used <i>before</i>/<i>outside</i> the
/// time engine (leave-day counting, quota projection, etc.).</description></item>
/// <item><description><b>This service counts from EVALUATED data</b> -- it sums columns on already-committed
/// <see cref="HCMv3.TimeManagement.TimeRegister"/> rows (<c>PublicHoliday</c>, <c>WeeklyOff</c>,
/// <c>RestDay</c>, <c>SickLeavePaidDay</c>, <c>SickLeaveUnpaidDay</c>, <c>AbsentDay</c>, <c>ActiveDay</c>).
/// Those columns are the <i>output</i> of the time engine and can legitimately differ from the master-data
/// answer: substitute duties, mid-period duty-period changes, leave that converted a rest day into an absent
/// day, manual TimeRegister corrections, and the approval workflow (<c>ApprovedAbsentDay</c> vs
/// <c>AbsentDay</c>) all move the evaluated value away from the configured value. Payroll must pay what was
/// evaluated, not what was configured.</description></item>
/// <item><description><b>IAccrualService.GetActualWeekOffsAsync</b> is a port of the legacy <i>XAF</i>
/// <c>AccrualService.GetActualWeekOffs</c>, which is a genuinely different legacy method from
/// <c>Program.GetWeeklyOffDays</c> ported here -- see <see cref="GetWeeklyOffDaysAsync"/> remarks for the
/// exact, itemised behavioural differences.</description></item>
/// </list>
/// </summary>
public interface IPayrollDayCountService : IDomainService
{
    /// <summary>
    /// Ported from legacy <c>Program.GetPresentDays</c> (<c>HCM10.PayrollEngine/Program.cs</c> line 4555).
    /// Produces wage-type <b>4014 "Present Days"</b>.
    ///
    /// <para>The legacy signature ended in <c>ref List&lt;SalaryRegisterDays&gt; salaryRegisterDaysList</c>
    /// (call site line 2201), which is illegal on an <c>async</c> method (CS1988). It is replaced by the
    /// <see cref="PresentDaysResult"/> return type -- see that class for the semantic consequences
    /// (append-to-shared-list becomes return-my-own-rows).</para>
    ///
    /// <para><b>Standalone, and the only method here that DELEGATES internally.</b> The top-level algorithm
    /// (fixed-days vs actual-days switching, mid-month join/leave detection, partial-period cut-off
    /// factoring, the annual-leave-no-impact correction) exists nowhere else in the solution and is ported
    /// whole. It does however delegate two sub-calculations:</para>
    /// <list type="bullet">
    /// <item><description>the weekly-off count it needs for <c>salaryBasisDays</c> goes through the private
    /// payroll-engine <c>GetPayrollActualWeekOffsAsync</c> helper, which in turn calls
    /// <see cref="GetWeeklyOffDaysAsync"/> on this same interface (legacy <c>Program.GetActualWeekOffs</c>
    /// line 2858 calls <c>Program.GetWeeklyOffDays</c> the same way). It is deliberately NOT delegated to
    /// <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/> -- see
    /// <see cref="GetWeeklyOffDaysAsync"/> remarks for the four itemised differences.</description></item>
    /// <item><description>the "non-working days before joining" count uses the private
    /// <c>GetNonWorkingWeekOffsAsync</c> helper (legacy <c>Program.GetNonWorkingWeekOffs</c> line 4853),
    /// which is a master-data duty walk. It is deliberately NOT delegated to
    /// <c>ITimeDataService.GetWeeklyOffDaysCountAsync</c> nor to <c>AccrualService</c>'s equivalent private
    /// loop, because both of those apply an <c>EmployeeSubstituteDuty</c> override that the legacy payroll
    /// variant does not -- delegating would silently change pay for substituted days.</description></item>
    /// </list>
    /// </summary>
    /// <param name="employeeId">Legacy <c>emp</c>.</param>
    /// <param name="payStartDate">Legacy <c>payStartDate</c>. Only its Year/Month are used (legacy narrows it
    /// to the 1st of its month immediately).</param>
    /// <param name="payEndDate">Legacy <c>payEndDate</c>. Used only as the seed for
    /// <c>GetRecoveryPeriods</c> on the advance-leave off-cycle path.</param>
    /// <param name="salaryRegisterId">Legacy <c>salaryRegister</c>; only <c>InPeriod</c>/<c>ForPeriod</c> are
    /// read (to gate the month-wise breakdown).</param>
    /// <param name="payrollRunType">Legacy <c>payrollRunType</c>.</param>
    /// <param name="offCycleEndDate">Legacy <c>offCycleEndDate</c>.</param>
    /// <param name="offCycleReason">Legacy <c>offCycleReasons</c>. Compare against
    /// <see cref="PayrollDayCountOffCycleReasons"/>.</param>
    Task<PresentDaysResult> GetPresentDaysAsync(
        int employeeId,
        DateTime payStartDate,
        DateTime payEndDate,
        int salaryRegisterId,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        string offCycleReason);

    /// <summary>
    /// Ported from legacy <c>Program.GetActiveDays</c> (line 4895). Sums
    /// <see cref="HCMv3.TimeManagement.TimeRegister.ActiveDay"/> over the pay window. Produces wage-type
    /// <b>4011 "Active Days"</b>.
    ///
    /// <para><b>Standalone</b> (no existing equivalent anywhere in the solution -- <c>ActiveDay</c> is a pure
    /// TimeRegister output column with no master-data counterpart).</para>
    ///
    /// <para><b>Off-cycle asymmetry, ported verbatim:</b> the OffCycle branch clamps
    /// <c>payEndDate = min(payEndDate, offCycleEndDate)</c>, i.e. it can only SHORTEN the window. Contrast
    /// <see cref="GetAbsentDaysAsync"/>/<see cref="GetPublicHolidayDaysAsync"/>/<see cref="GetRestDaysAsync"/>,
    /// which unconditionally ASSIGN <c>payEndDate = offCycleEndDate</c> and can therefore EXTEND it. This
    /// inconsistency exists in the legacy source and is preserved deliberately; do not "fix" it without a
    /// payroll-parity test.</para>
    /// </summary>
    Task<double> GetActiveDaysAsync(
        int employeeId,
        DateTime payStartDate,
        DateTime payEndDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>Program.GetAbsentDays</c> (line 5042). Sums <c>AbsentDay</c> (or
    /// <c>ApprovedAbsentDay</c> when <c>ManageTimeSettings.EnableTimeRegisterApproval</c> is on) over the pay
    /// window, excluding days that already carry an unpaid-leave day (legacy comment: "If both UnpaidLeaveDay
    /// and AbsentDay for same day, then count only Absent because UnpaidLeaveDay already counted for that
    /// day"). Produces wage-type <b>4001/4002 "Absent (Unpaid) Days"</b>.
    ///
    /// <para><b>Standalone.</b> <see cref="IEmployeeAbsenceCalculationService"/>.<c>ActualLeaveDaysAsync</c>
    /// answers a different question -- "how many chargeable leave days does this <i>leave request</i> span,
    /// given weekend/holiday/rest-day inclusion flags on its AbsenceType" -- from EmployeeAbsence + master
    /// calendar data. This method counts <i>unauthorised absence</i> as recorded by the time engine per day,
    /// including the approval-workflow variant, which has no EmployeeAbsence row at all.</para>
    /// </summary>
    /// <param name="wageTypeCode">Legacy <c>WageCode</c>. Used only to look up
    /// <c>WageType.IncludeInCutoff</c> for the cut-off window shift.</param>
    /// <param name="payrollAreaId">Legacy <c>empPayrollArea</c>; only <c>CutOffDay</c> is read.</param>
    Task<double> GetAbsentDaysAsync(
        int employeeId,
        DateTime payStartDate,
        DateTime payEndDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        string wageTypeCode,
        int payrollAreaId);

    /// <summary>
    /// Ported from legacy <c>Program.GetPublicHolidayDays</c> (line 5073). Sums
    /// <see cref="HCMv3.TimeManagement.TimeRegister.PublicHoliday"/> from the 1st of
    /// <paramref name="payDate"/>'s month to <paramref name="payDate"/> (or to
    /// <paramref name="offCycleEndDate"/> on off-cycle runs). Produces wage-type <b>4007</b>.
    ///
    /// <para><b>Standalone -- deliberately NOT delegated to
    /// <c>ITimeDataService.GetPublicHolidaysDatesAsync</c>.</b> That method returns the list of dates that
    /// <i>are</i> public holidays for the employee's duty/holiday-calendar configuration, computed from
    /// master data, and returns whole dates (count = integer). This method sums a <c>double</c> column the
    /// time engine already wrote, which supports fractional values (half-day public holidays) and reflects
    /// per-day overrides, substitutions and manual TimeRegister edits that the master-data view cannot see.
    /// It also returns 0 for days with no TimeRegister row at all -- payroll must not pay a holiday that was
    /// never evaluated -- whereas the master-data method happily returns dates outside any evaluated
    /// period.</para>
    /// </summary>
    Task<double> GetPublicHolidayDaysAsync(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>Program.GetWeeklyOffDays</c> (line 5103). Produces wage-type <b>4008 "Weekly Off
    /// Days"</b>. Two branches, selected by wage type 4008's own <c>BillingMethod</c>:
    /// <list type="number">
    /// <item><description><c>BillingMethod.WorkingDaysPerMonth</c>: walk the employee's TimeRegister rows for
    /// the month-to-<paramref name="payDate"/> window and count those whose resolved <c>Duty.IsWeeklyOff</c>
    /// is true (roster-resolved via <see cref="IRosterService"/> when
    /// <c>ManageTimeSettings.EnableRosterDuty</c> is on, else the row's own <c>Duty</c> FK). Throws when no
    /// TimeRegister rows exist and the employee has no overlapping <c>VariableTime</c>
    /// <c>EmployeeTimePolicy</c>.</description></item>
    /// <item><description>any other BillingMethod: simply sum
    /// <see cref="HCMv3.TimeManagement.TimeRegister.WeeklyOff"/>.</description></item>
    /// </list>
    ///
    /// <para><b>Standalone -- deliberately NOT delegated to
    /// <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>, and NOT to
    /// <c>ITimeDataService.GetWeeklyOffDaysCountAsync</c>.</b> These are three different legacy methods
    /// answering three different questions. Precise differences:</para>
    /// <list type="bullet">
    /// <item><description><b>vs <c>ITimeDataService.GetWeeklyOffDaysCountAsync</c></b> (legacy
    /// <c>TimeDataService.GetWeeklyOffDaysCount</c>): that one is pure master data -- it iterates every
    /// calendar day in the range and resolves the configured duty, ignoring TimeRegister entirely. It answers
    /// "how many weekly-offs is this employee <i>scheduled</i> for". This method answers "how many weekly-offs
    /// did the time engine actually <i>record</i>", and returns 0 for unevaluated days.</description></item>
    /// <item><description><b>vs <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/></b>
    /// (legacy XAF <c>AccrualService.GetActualWeekOffs</c>): four concrete differences. (a) <b>Window</b>:
    /// the accrual one always covers the FULL calendar month (1st..last day); this one covers
    /// 1st..<paramref name="payDate"/>, or 1st..<paramref name="offCycleEndDate"/> on off-cycle runs -- i.e.
    /// a partial period. (b) <b>Missing-day fallback</b>: the accrual one back-fills days that have no
    /// TimeRegister row by resolving their duty day-by-day; this one does NOT back-fill -- an unevaluated day
    /// contributes nothing. (c) <b>Counting rule</b>: the accrual one counts rows where <c>WeeklyOff &gt; 0</c>
    /// (a row count, so a 0.5 weekly-off counts as 1); this one either sums the <c>WeeklyOff</c> column
    /// (fraction-preserving) or counts <c>Duty.IsWeeklyOff</c>, depending on wage type 4008's BillingMethod --
    /// a switch the accrual one does not have at all. (d) <b>Failure mode</b>: the accrual one throws when a
    /// missing day's duty cannot be resolved; this one throws when the whole window has no TimeRegister rows
    /// and no VariableTime policy. They are NOT interchangeable and must not be collapsed.</description></item>
    /// <item><description><b>vs the OTHER accrual overload,
    /// <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime, int)"/></b> (legacy XAF
    /// <c>AccrualService.GetActualWeekOffs(session, emp, empTimeRegister, payDate, salaryRegisterDetail)</c>):
    /// all four differences above apply to it as well, plus a fifth. That overload takes a
    /// <c>salaryRegisterDetailId</c> and is a <i>wage-type amount</i> calculation -- it exists to value the
    /// weekly-off wage type on an already-persisted <see cref="SalaryRegisterDetail"/> row, so it needs the
    /// detail's rate/period context. This method takes no salary-register argument at all: it produces the
    /// raw <c>Number</c> for wage type 4008 <i>before</i> any detail row exists. Using the accrual overload
    /// here would be a chicken-and-egg inversion, not merely a numeric difference.</description></item>
    /// </list>
    ///
    /// <para><b>The root cause of the divergence, stated plainly:</b> this method reads
    /// already-evaluated <see cref="HCMv3.TimeManagement.TimeRegister"/> output, while
    /// <c>ITimeDataService.GetWeeklyOffDaysCountAsync</c> recomputes from raw duty/roster master data and
    /// <see cref="IAccrualService"/>'s overloads do a hybrid (TimeRegister first, master-data back-fill for
    /// gaps). That is a <i>real</i>, intended behavioural difference, not an accident of porting: payroll
    /// pays what the time engine recorded. Any future attempt to make all three agree must be done as a
    /// deliberate, tested payroll-parity change, never as a refactor.</para>
    /// </summary>
    Task<double> GetWeeklyOffDaysAsync(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        string offCycleReason);

    /// <summary>
    /// Ported from legacy <c>Program.GetRestDays</c> (line 5196). Sums
    /// <see cref="HCMv3.TimeManagement.TimeRegister.RestDay"/> over the month-to-<paramref name="payDate"/>
    /// window, EXCLUDING rows that also carry an absent day (legacy comment: "This is done if
    /// NotReturnFromLeave, then RestDay is Absent"). Produces wage-type <b>4009</b>.
    ///
    /// <para><b>Standalone -- deliberately NOT delegated to <c>ITimeDataService.GetRestDaysCountAsync</c>/
    /// <c>GetRestDaysListAsync</c>.</b> Same master-vs-evaluated split as
    /// <see cref="GetPublicHolidayDaysAsync"/>, plus one payroll-specific rule that has no master-data
    /// equivalent and cannot exist in the TimeManagement variant: the absent-day exclusion. A configured rest
    /// day that the employee failed to return from leave for is recorded by the time engine as an absent day,
    /// and payroll must not pay it as a rest day. <c>ITimeDataService</c> has no notion of absence at
    /// all.</para>
    /// </summary>
    Task<double> GetRestDaysAsync(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>Program.GetSickLeavePaidDays</c> (line 5227). Sums
    /// <see cref="HCMv3.TimeManagement.TimeRegister.SickLeavePaidDay"/>. Produces wage-type <b>4003</b>.
    ///
    /// <para><b>Standalone -- deliberately NOT delegated to
    /// <see cref="IEmployeeAbsenceCalculationService.ActualLeaveDaysAsync"/>.</b> That is the only other
    /// "sick leave days" calculation in the solution, and it answers a different question: "how many
    /// chargeable days does this leave <i>request</i> span", derived from <c>EmployeeAbsence</c> start/end
    /// dates and the AbsenceType's weekend/holiday/rest-day inclusion flags over master calendar data. It
    /// returns a single <c>decimal</c> for a whole request and has no notion of the paid/unpaid split at
    /// all.</para>
    ///
    /// <para>This method instead sums the per-day paid/unpaid split the time engine already applied and
    /// wrote to <see cref="HCMv3.TimeManagement.TimeRegister"/>. That split depends on how much of the
    /// employee's sick-leave slab entitlement had been consumed at the moment each individual day was
    /// evaluated -- a single 30-day sick leave typically straddles several slabs and lands partly in
    /// <c>SickLeavePaidDay</c> and partly in <c>SickLeaveUnpaidDay</c>. That information is not recoverable
    /// from the leave request alone, so the reconciliation is: <b>same underlying absence, different
    /// question, and only the evaluated data can answer this one.</b> This is the reads-evaluated-data vs
    /// recomputes-from-master-data difference again, and it is real and intended.</para>
    ///
    /// <para><b>Off-cycle clamping:</b> uses the <c>min(payEndDate, offCycleEndDate)</c> shortening form (as
    /// <see cref="GetActiveDaysAsync"/>), not the unconditional-assign form. Ported verbatim.</para>
    /// </summary>
    Task<double> GetSickLeavePaidDaysAsync(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>Program.GetSickLeaveUnpaidDays</c> (line 5254). Identical in shape to
    /// <see cref="GetSickLeavePaidDaysAsync"/> but sums
    /// <see cref="HCMv3.TimeManagement.TimeRegister.SickLeaveUnpaidDay"/>. Produces wage-type <b>4005</b>.
    /// <b>Standalone</b>, for the same reasons.
    /// </summary>
    Task<double> GetSickLeaveUnpaidDaysAsync(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>Program.GetPaidDays</c> (line 5282). Produces wage-type <b>4015 "Paid Days"</b>
    /// as the sum of three already-calculated <see cref="SalaryRegisterDetail"/> <c>Number</c> values on the
    /// SAME salary register: <b>4014</b> (Present Days) + <b>4002</b> (Paid Leave Days) + <b>4000</b> (Public
    /// Holiday / first time type). It touches no TimeRegister data at all.
    ///
    /// <para><b>Standalone by definition</b> -- it is a roll-up of this run's own output rows, so it MUST be
    /// invoked only after the 4014/4002/4000 details have been written for
    /// <paramref name="salaryRegisterId"/>. Missing details contribute 0 (legacy
    /// <c>.FirstOrDefault()</c> on <c>double</c>).</para>
    ///
    /// <para>The legacy signature's <c>objectSpace</c>/<c>emp</c>/<c>payStartDate</c>/<c>payEndDate</c>
    /// parameters are dropped: the legacy body never reads any of them (verified against lines 5282-5298).</para>
    /// </summary>
    Task<double> GetPaidDaysAsync(int salaryRegisterId);
}
