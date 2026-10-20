using System;
using System.Collections.Generic;
using HCMv3.SystemSettings;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The three off-cycle reason strings the legacy engine compares against by VALUE, not by id
/// (legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 148-150:
/// <c>endOfServiceOffcycle</c>/<c>reasonAdvanceLeave</c>/<c>reasonLeaveEncashment</c>).
///
/// <para>Legacy passed the reason in as <c>args[6]</c> (a raw string) and matched it against a
/// <c>PayrollReasonType</c> row by <c>NameEn == offCycleReasons || NameAr == offCycleReasons</c>
/// (legacy lines 1313-1314 and 1338-1339). That string-matching contract is preserved verbatim here --
/// <c>PayrollEngineRunInput.OffCycleReason</c> is a string, and these constants are the exact literals
/// the branching logic keys off. Do NOT "improve" this into an enum without also migrating the
/// <c>PayrollReasonType</c> lookup rows, or off-cycle branching will silently take the wrong arm.</para>
/// </summary>
public static class PayrollOffCycleReasons
{
    /// <summary>Legacy <c>Program.endOfServiceOffcycle</c> (line 148).</summary>
    public const string EndOfService = "End Of Service";

    /// <summary>Legacy <c>Program.reasonAdvanceLeave</c> (line 149).</summary>
    public const string AdvanceLeave = "Advance Leave";

    /// <summary>Legacy <c>Program.reasonLeaveEncashment</c> (line 150).</summary>
    public const string LeaveEncashment = "Leave Encashment";
}

/// <summary>
/// The handful of wage-type codes the ENGINE ITSELF (not an individual wage rule) branches on. The ~34
/// wage-rule codes of waves P4-P6 belong on their own <c>IWageTypeCalculator.WageCode</c> values, not
/// here.
/// </summary>
public static class PayrollEngineWageCodes
{
    /// <summary>
    /// "Advance Salary Recovery Deduction". The engine zeroes any existing 2005 line and any matching
    /// <c>EmployeePayment</c> rows before an off-cycle run (legacy <c>Program.cs</c> lines 1632-1636,
    /// 1788-1793, 1800-1803).
    /// </summary>
    public const string AdvancePaymentDeduction = "2005";

    /// <summary>
    /// "Last Month Recovery Deduction". Cleared on re-run of the same month (legacy <c>Program.cs</c>
    /// lines 1824-1829, 1836-1838).
    /// </summary>
    public const string LastMonthRecoveryDeduction = "2011";

    /// <summary>
    /// "Net Pay" -- the terminal wage type. Legacy calls <c>Program.SetRoundoff(salaryRegister)</c> as the
    /// FIRST statement of its 9000 branch (legacy
    /// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> line 1062), so rounding happens BEFORE
    /// net pay is summed. See <see cref="IPayrollRoundingService"/>.
    /// </summary>
    public const string NetPay = "9000";
}

/// <summary>
/// Immutable snapshot of the payroll-run configuration flags legacy held as <c>public static</c> fields on
/// <c>Program</c> (legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 110-141) and populated once per run in
/// <c>GetInitialVariables</c> (lines 2622-2712).
///
/// <para><b>Read once, passed down.</b> The engine resolves this from <see cref="SystemSettings.Services.ISysSettingsAccessor"/>
/// exactly once at the start of a run and hands it to every wage rule through
/// <see cref="PayrollCalculationContext.Settings"/>. Wage rules (waves P4-P6) MUST read flags from here and
/// MUST NOT inject <c>ISysSettingsAccessor</c> themselves -- legacy semantics are "one snapshot for the whole
/// run", and re-reading mid-run would let a concurrent settings edit split a single payroll run across two
/// configurations.</para>
///
/// <para>Only the flags the legacy engine actually consumed are carried. Where a legacy field was a hardcoded
/// literal never assigned from settings (<c>calculateSalRegDaysDetail = true</c>, legacy line 131 -- verified
/// never re-assigned), it is NOT carried as a flag; see <see cref="PresentDaysResult.MonthWiseDays"/> remarks.</para>
/// </summary>
public class PayrollEngineSettings
{
    /// <summary>Legacy <c>isRosterActive</c> = <c>ManageTimeSettings.EnableRosterDuty</c> (line 2630).</summary>
    public bool IsRosterActive { get; set; }

    /// <summary>Legacy <c>isPeriodwiseProvision</c> = <c>ManagePayrollSettings.LeaveProvisionAsPerCurrentSalary</c>.</summary>
    public bool IsPeriodwiseProvision { get; set; }

    /// <summary>Legacy <c>isNetPayRound</c> = <c>ManagePayrollSettings.RoundNetPay</c>. See <see cref="IPayrollRoundingService.GetNetPayRoundingDigitsAsync"/>.</summary>
    public bool IsNetPayRound { get; set; }

    /// <summary>Legacy <c>apply_Annualleavenoimpact_Logic</c> = <c>ApplyAnnualleaveNoImpactLogic</c>.</summary>
    public bool ApplyAnnualLeaveNoImpactLogic { get; set; }

    /// <summary>Legacy <c>isNotReturnFromLeaveLogicEnabled</c> = <c>EnableNotReturnFromLeaveLogic</c>.</summary>
    public bool IsNotReturnFromLeaveLogicEnabled { get; set; }

    /// <summary>Legacy <c>generate_Register_On_NotReturnfromleave</c> = <c>GenerateRegisterOnNotReturnFromLeave</c>.</summary>
    public bool GenerateRegisterOnNotReturnFromLeave { get; set; }

    /// <summary>Legacy <c>enableBasicpayMoreThenFixeddays</c> = <c>EnableBasicpayMoreThenFixedDays</c>.</summary>
    public bool EnableBasicPayMoreThanFixedDays { get; set; }

    /// <summary>Legacy <c>enableUnpaidDedSlabWise</c> = <c>EnableUnpaidDedSlabWise</c>.</summary>
    public bool EnableUnpaidDedSlabWise { get; set; }

    /// <summary>Legacy <c>default_Working_Hours</c> = <c>DefaultWorkingHours</c>.</summary>
    public double DefaultWorkingHours { get; set; }

    /// <summary>
    /// Legacy <c>decimal6RoundingFrom</c> = <c>Decimal6RoundingFrom</c>. Registers whose
    /// <c>ForPeriod</c> is strictly BEFORE this date use the old currency-decimal rounding; everything else
    /// uses <see cref="DecimalInSalaryRegister"/>. See <see cref="IPayrollRoundingService"/>.
    /// </summary>
    public DateTime Decimal6RoundingFrom { get; set; }

    /// <summary>
    /// Legacy <c>decimalInSalaryRegister</c> = <c>DecimalInSalaryRegister</c>, with legacy's exact zero
    /// guard applied at load time: <c>(DecimalInSalaryRegister == 0) ? 6 : DecimalInSalaryRegister</c>
    /// (legacy line 2640).
    /// </summary>
    public int DecimalInSalaryRegister { get; set; } = 6;

    /// <summary>Legacy <c>fullMonthLeaveAdjForFixedDays</c>.</summary>
    public bool FullMonthLeaveAdjForFixedDays { get; set; }

    /// <summary>Legacy <c>payLeaveAsPerCutoff</c>. NOTE legacy's field default is <c>true</c> (line 122).</summary>
    public bool PayLeaveAsPerCutoff { get; set; } = true;

    /// <summary>Legacy <c>enableAuditLogInPayrollEngine</c> = <c>ManageSecuritySettings.EnableAuditLogInPayrollEngine</c>.</summary>
    public bool EnableAuditLogInPayrollEngine { get; set; }

    /// <summary>Legacy <c>fixedToActualWhenLeaveOverlapOnMonthStart</c>.</summary>
    public bool FixedToActualWhenLeaveOverlapOnMonthStart { get; set; }

    /// <summary>Legacy <c>fixedToActualWhenLeaveOverlapOnMonthEnd</c>.</summary>
    public bool FixedToActualWhenLeaveOverlapOnMonthEnd { get; set; }

    /// <summary>Legacy <c>fixedToActualWhenUpaidLeaveFound</c> (legacy typo preserved in meaning: UNPAID leave).</summary>
    public bool FixedToActualWhenUnpaidLeaveFound { get; set; }

    /// <summary>Legacy <c>fixedToActualWhenAbsentDayFound</c>.</summary>
    public bool FixedToActualWhenAbsentDayFound { get; set; }

    /// <summary>Legacy <c>overtimeHoursBasis</c>.</summary>
    public OvertimeHoursBasis OvertimeHoursBasis { get; set; }

    /// <summary>Legacy <c>forfeitRule</c> = <c>ManageTimeSettings.ForfeitRule</c>.</summary>
    public ForfeitRule ForfeitRule { get; set; }

    /// <summary>Legacy <c>socialSecurityRateRule</c>.</summary>
    public SocialSecurityRateRule SocialSecurityRateRule { get; set; }

    /// <summary>Legacy <c>enableTimeRegisterApproval</c> = <c>ManageTimeSettings.EnableTimeRegisterApproval</c>.</summary>
    public bool EnableTimeRegisterApproval { get; set; }

    /// <summary>
    /// Legacy <c>enableTimeEngineAutorun</c> = <c>ManageTimeSettings.EnableTimeEngineAutorun</c>. Selects
    /// which employee-selection branch and which control-record guard the run uses -- see
    /// <see cref="IPayrollEngineService"/>.
    /// </summary>
    public bool EnableTimeEngineAutorun { get; set; }

    /// <summary>Legacy <c>runOffCycleForFutureDate</c>.</summary>
    public bool RunOffCycleForFutureDate { get; set; }

    /// <summary>Legacy <c>regularLeaveEncashOnBasicPayOnly</c>.</summary>
    public bool RegularLeaveEncashOnBasicPayOnly { get; set; }

    /// <summary>Legacy <c>fnfLeaveEncashOnBasicPayOnly</c>.</summary>
    public bool FnFLeaveEncashOnBasicPayOnly { get; set; }

    /// <summary>Legacy <c>changePartialPeriodFactoringInFixedDaysBilling</c>.</summary>
    public bool ChangePartialPeriodFactoringInFixedDaysBilling { get; set; }

    /// <summary>Legacy <c>partialPeriodCutoffDayInFixedDaysBilling</c>.</summary>
    public int PartialPeriodCutoffDayInFixedDaysBilling { get; set; }

    /// <summary>Legacy <c>includeSalaryIncProvDiffInSalReg</c>.</summary>
    public bool IncludeSalaryIncProvDiffInSalReg { get; set; }

    /// <summary>Legacy <c>includeSalaryIncProvDiffInSalRegEffDt</c>.</summary>
    public DateTime IncludeSalaryIncProvDiffInSalRegEffDt { get; set; }

    /// <summary>Legacy <c>lockSalaryRegisterOnPayrollExit</c>.</summary>
    public bool LockSalaryRegisterOnPayrollExit { get; set; }

    /// <summary>Legacy <c>payrollSettings.PickOffcyclePayment</c> (used at legacy line 594).</summary>
    public PickOffcyclePayment PickOffcyclePayment { get; set; }

    /// <summary>
    /// Legacy <c>earliestEvaluationDate</c> = <c>ManageTimeSettings.EarliestEvaluationDate</c> (legacy line
    /// 539 -- read separately from <c>GetInitialVariables</c>, folded into this snapshot here).
    /// </summary>
    public DateTime EarliestEvaluationDate { get; set; }

    /// <summary>
    /// Legacy <c>Program.AnnualLeaveOid</c> (declared line 143, resolved at the END of
    /// <c>GetInitialVariables</c>, line 2710: <c>AbsenceTypeList.Where(r =&gt; r.Code == "1")</c>) -- i.e. the
    /// <c>AbsenceType</c> whose <b>Code is the literal string "1"</b> is "annual leave". Several legacy leave
    /// rules test <c>r.AbsenceType.Oid == AnnualLeaveOid || r.AbsenceType.QuotaType == QuotaType.AnnualLeave</c>
    /// (legacy lines 3170, 3229, 3236, 3263, 3294, 3301) -- note the OR: the code-"1" row and the
    /// <c>QuotaType.AnnualLeave</c> flag are NOT assumed to coincide, and a ported rule must keep both arms.
    ///
    /// <para>Resolved once per run and carried here for the same reason as every other flag on this class.
    /// <c>0</c> when no <c>AbsenceType</c> with code "1" exists -- legacy's own <c>FirstOrDefault</c>
    /// default, which makes the first arm of that OR simply never match.</para>
    /// </summary>
    public int AnnualLeaveAbsenceTypeId { get; set; }
}

/// <summary>
/// The seven per-employee on-hold gates legacy resolved once per employee from the employee's active
/// <c>EmployeeOnHold</c> row (legacy <c>Program.cs</c> lines 860-896) and then threaded through as seven
/// separate <c>bool</c> parameters on every <c>*PayService.Main</c> signature.
///
/// <para>Legacy's <c>onHold_All == true</c> fan-out (lines 885-894, "all seven become true") is applied by
/// <see cref="PayrollEngineService"/> when it builds this object, so consumers only ever see the seven
/// resolved flags -- exactly what the legacy <c>Main</c> signatures received. <see cref="All"/> is kept
/// because the engine (not the rules) still branches on it directly at legacy lines 961, 1086 and 1101.</para>
/// </summary>
public class PayrollOnHoldFlags
{
    /// <summary>Legacy <c>onHold_All</c> (<c>EmployeeOnHold.OnHold</c>).</summary>
    public bool All { get; set; }

    /// <summary>Legacy <c>onHold_BasicPay</c>.</summary>
    public bool BasicPay { get; set; }

    /// <summary>Legacy <c>onHold_LeavePay</c>.</summary>
    public bool LeavePay { get; set; }

    /// <summary>Legacy <c>onHold_IndemnityPay</c>.</summary>
    public bool IndemnityPay { get; set; }

    /// <summary>Legacy <c>onHold_AirTicketPay</c>.</summary>
    public bool AirTicketPay { get; set; }

    /// <summary>Legacy <c>onHold_LoanDeduction</c>.</summary>
    public bool LoanDeduction { get; set; }

    /// <summary>Legacy <c>onHold_AllEarnings</c>.</summary>
    public bool AllEarnings { get; set; }

    /// <summary>Legacy <c>onHold_AllDeductions</c>.</summary>
    public bool AllDeductions { get; set; }

    /// <summary>
    /// Legacy lines 1387: the "did ANY hold apply" test used to decide whether to reset
    /// <c>EmployeePayrollStatus.AccountedTo</c>/<c>EarliestMasterDataChange</c> after the run.
    /// </summary>
    public bool AnyHold =>
        All || BasicPay || AirTicketPay || IndemnityPay || LeavePay || LoanDeduction || AllEarnings || AllDeductions;
}

/// <summary>
/// Identifies the register row a calculation writes to, WITHOUT naming a concrete entity type -- so the
/// same wage rule works against <c>SalaryRegister</c> and (wave P7) <c>SimulationSalaryRegister</c>.
/// See <see cref="PayrollRegisterTarget"/> for why.
/// </summary>
public class PayrollRegisterRef
{
    /// <summary>
    /// Primary key of the <c>SalaryRegister</c> (or, when <see cref="Target"/> is
    /// <see cref="PayrollRegisterTarget.Simulation"/>, of the <c>SimulationSalaryRegister</c>) row.
    /// </summary>
    public int RegisterId { get; set; }

    /// <summary>Which register family <see cref="RegisterId"/> refers to.</summary>
    public PayrollRegisterTarget Target { get; set; }

    /// <summary>Legacy <c>salaryRegister.ForPeriod</c> -- carried so rules need no extra round-trip.</summary>
    public DateTime ForPeriod { get; set; }

    /// <summary>Legacy <c>salaryRegister.InPeriod</c> -- carried so rules need no extra round-trip.</summary>
    public DateTime InPeriod { get; set; }

    /// <summary>Legacy <c>salaryRegister.PayrollRunType</c>.</summary>
    public PayrollRunType PayrollRunType { get; set; }
}

/// <summary>
/// Target-agnostic mirror of one register line -- structurally the intersection of
/// <see cref="SalaryRegisterDetail"/> and <see cref="SimulationSalaryRegisterDetail"/> (confirmed
/// identical: WageTypeId/Rate/Number/Amount). Wage rules read and write these instead of the entities, so
/// the live/simulation branch lives in exactly one place (<see cref="IPayrollRegisterStore"/>).
/// </summary>
public class PayrollRegisterLine
{
    /// <summary>Primary key of the underlying detail row; 0 for a line not yet persisted.</summary>
    public int Id { get; set; }

    public int WageTypeId { get; set; }

    /// <summary>Denormalised <c>WageType.Code</c> -- legacy matched register lines by code constantly.</summary>
    public string WageCode { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }

    /// <summary>
    /// Legacy <c>salaryRegisterDetail.IsSystemGenerated</c>. Every line the engine writes is system
    /// generated, so <see cref="IPayrollRegisterStore.SaveLineAsync"/> stamps it <c>true</c> on save;
    /// reads surface whatever is stored.
    /// </summary>
    public bool IsSystemGenerated { get; set; }
}

/// <summary>
/// Everything legacy's <c>Main(string[] args)</c> parsed out of <c>args[0..8]</c>
/// (legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 168-215). The <c>args[3]</c> "simulation run" flag
/// becomes <see cref="RegisterTarget"/>; <c>args[7]</c> (<c>currentAspect</c>, an XAF UI culture code used
/// only to pick between hardcoded English and Arabic exception strings) and <c>args[8]</c>
/// (<c>currentUserName</c>, used only for legacy's manual <c>SysCreator</c>/<c>SysModifier</c> stamping,
/// which ABP's <c>FullAuditedEntity</c> now does automatically) are NOT ported.
/// </summary>
public class PayrollEngineRunInput
{
    /// <summary>Legacy <c>args[0]</c>: "O" =&gt; OffCycle, anything else =&gt; Regular (legacy lines 187-191).</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>args[1]</c>: a COMMA-SEPARATED list of employee codes, or blank to mean "every employee in
    /// the payroll area" (legacy splits on ',' at lines 441, 528, 1477).
    /// </summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>args[2]</c>, parsed as dd/MM/yyyy. Required (legacy lines 218-224).</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>args[4]</c>: the <see cref="PayrollArea.Name"/> (NOT Code) the run is scoped to.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[5]</c>, parsed as dd/MM/yyyy. Required for off-cycle runs.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>
    /// Legacy <c>args[6]</c>. Must be non-blank for off-cycle runs (legacy lines 234-237) and is matched by
    /// value against <see cref="PayrollOffCycleReasons"/>.
    /// </summary>
    public string OffCycleReason { get; set; }

    /// <summary>
    /// Legacy <c>args[3]</c> ("0" = live run, "1" = simulation run), which legacy used to pick between
    /// <c>Program</c> and <c>SimulationPayrollHelper</c>. See <see cref="PayrollRegisterTarget"/>.
    /// </summary>
    public PayrollRegisterTarget RegisterTarget { get; set; } = PayrollRegisterTarget.Live;

    /// <summary>
    /// Save the run this many employees at a time, each batch in its own unit of work
    /// (<see cref="PayrollEngineBatches"/>). 0 (the default) keeps the whole run in the caller's unit of work, as
    /// before -- which is what Go-Live Setup and the autorun worker rely on, since they batch themselves or
    /// expect one call to be one transaction. The everyday Payroll Engine job passes
    /// <see cref="PayrollEngineBatches.Size"/>.
    ///
    /// <para>Not a legacy argument. Legacy committed per employee (an object space per employee, legacy
    /// <c>Program.cs</c> lines 756/1462); a batch of ten is the Go-Live wizard's proven compromise between that
    /// and one transaction for the whole run.</para>
    /// </summary>
    public int CommitBatchSize { get; set; }
}

/// <summary>
/// How many employees an everyday payroll run saves at a time. The same number and the same reason as
/// <c>GoLiveEngineBatches.Size</c> (Go-Live Setup, IT11): the engine keeps everything it writes for one unit of
/// work in memory until the unit of work ends, so one unit of work for a whole payroll area gets slower with
/// every employee (186 employees went from 3.5 to 9 seconds each) and a failure late in the run threw away all
/// of it. Ten at a time keeps every employee as quick as the first, and a failure costs its batch only.
/// </summary>
public static class PayrollEngineBatches
{
    public const int Size = 10;
}

/// <summary>
/// One entry of <see cref="PayrollEngineRunResult.EmployeeResults"/> -- the in-memory equivalent of one
/// legacy <c>PayrollLog</c> row (legacy <c>Program.cs</c> lines 1419-1428 for success, 1444-1453 for
/// failure).
///
/// <para>NOT PORTED IN P1: actually persisting <c>PayrollLog</c>/<c>ExecutionLog</c> rows. Those belong to
/// the later unified app-service/background-job wave (this wave is domain-layer only), which will translate
/// these results into rows. Legacy's "swallow the per-employee exception, log it, continue to the next
/// employee" control flow IS ported -- see <see cref="PayrollEngineService"/>.</para>
/// </summary>
public class PayrollEmployeeRunResult
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    /// <summary>Legacy <c>PayrollLog.Status</c>: 0 = success, 1 = failed.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Legacy <c>PayrollLog.Message</c>. Legacy truncates to 1000 chars (line 1447) -- preserved.</summary>
    public string Message { get; set; }

    /// <summary>Ids of the register rows created/refreshed for this employee (one per retro period).</summary>
    public List<int> SalaryRegisterIds { get; set; } = new List<int>();
}

/// <summary>Aggregate outcome of one <see cref="IPayrollEngineService.RunAsync"/> call.</summary>
public class PayrollEngineRunResult
{
    /// <summary>The resolved pay-in date actually used, AFTER the control-record adjustments of legacy lines 296-297 / 336.</summary>
    public DateTime PayInDate { get; set; }

    public List<PayrollEmployeeRunResult> EmployeeResults { get; set; } = new List<PayrollEmployeeRunResult>();

    /// <summary>
    /// True when the run stopped early because a <c>PayrollLog</c> row with <c>StopFlag</c> was found
    /// (legacy <c>Program.cs</c> lines 776-777).
    /// </summary>
    public bool Aborted { get; set; }
}
