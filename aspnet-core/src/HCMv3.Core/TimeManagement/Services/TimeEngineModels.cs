using System;
using System.Collections.Generic;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Input for a Time Engine run (ported from the legacy <c>HCM10.TimeEngine</c> console app's
/// <c>Main(string[] args)</c> argument list -- <c>args[0]</c>=employee code CSV, <c>args[1]</c>=start date,
/// <c>args[2]</c>=end date, <c>args[3]</c>=payroll area, <c>args[4]</c>=current user name,
/// <c>args[5]</c>="1" for "ran from payroll engine"). The legacy <c>dd/MM/yyyy</c> string parsing and the
/// connection-string/XAF type-registration plumbing around it are deliberately NOT ported -- that was
/// console-host wiring, not algorithm.
/// </summary>
public class TimeEngineRunInput
{
    /// <summary>Legacy <c>args[0]</c> (<c>empCode</c>). Comma-separated employee codes; empty/null = all.</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>
    /// Legacy <c>args[1]</c> (<c>timeStartDate</c>). Null = legacy's <c>DateTimeExtension.DBDtNull</c> sentinel,
    /// which <c>CreateTimeRegister</c> replaces with the 1st of the current month.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Legacy <c>args[2]</c> (<c>timeEndDate</c>). Required -- legacy throws "No End Date defined for
    /// Time Calculation or Check Date format." when missing.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Legacy <c>args[3]</c> (<c>payrollArea</c>). Matched against <c>PayrollArea.Name</c>.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[4]</c> (<c>currentUserName</c>), recorded on the <see cref="SystemSecurity.ExecutionLog"/>.</summary>
    public string CurrentUserName { get; set; }

    /// <summary>Legacy <c>args[5]</c> (<c>IsRanFromPayrollEngine</c>).</summary>
    public bool IsRanFromPayrollEngine { get; set; }

    /// <summary>
    /// PORT ADAPTATION (no legacy equivalent). Legacy resolved the employee's EmployeeSubGroup from
    /// <c>EmployeeOrgAssignment.EmployeeSubGroup</c>, but <see cref="EmployeeOrgAssignment"/> in this port
    /// carries NO <c>EmployeeSubGroupId</c> FK (documented on that entity and already worked around the same
    /// way by <c>IAccrualService</c>, which takes <c>employeeSubGroupId</c> as an explicit caller-supplied
    /// parameter). Callers supply the mapping here: employeeId -&gt; EmployeeSubGroup id. Employees missing
    /// from this map behave exactly like legacy's "Employee Sub Group not found for Employee." error path.
    /// </summary>
    public Dictionary<int, int> EmployeeSubGroupIds { get; set; } = new Dictionary<int, int>();
}

/// <summary>
/// Outcome summary of a Time Engine run, mirroring the values legacy wrote back onto its
/// <see cref="SystemSecurity.ExecutionLog"/> row in <c>CreateTimeRegister</c>'s <c>finally</c> block.
/// </summary>
public class TimeEngineRunResult
{
    public int ExecutionLogId { get; set; }

    public int TotalRecords { get; set; }

    public int Successfull { get; set; }

    public int Failed { get; set; }

    public SystemSecurity.ExecutionLogStatus ExecutionStatus { get; set; }

    /// <summary>True when the run stopped early because a <see cref="TimeLog"/> row had <c>StopFlag</c> set.</summary>
    public bool Aborted { get; set; }
}

/// <summary>
/// Snapshot of the ~25 legacy <c>Program</c> static configuration flags (legacy <c>Program.cs</c> lines
/// ~79-119, populated by <c>GetInitialVariables</c>). Every value here comes from
/// <see cref="HCMv3.SystemSettings.Services.ISysSettingsAccessor"/> -- the engine never re-reads settings
/// independently.
/// </summary>
public class TimeEngineSettings
{
    /// <summary>Legacy <c>isAutoAttendanceEnabled</c> = <c>ManageTimeSettings.EnableAttendanceOnHoliday</c>.</summary>
    public bool IsAutoAttendanceEnabled { get; set; }

    /// <summary>Legacy <c>isRosterActive</c> = <c>ManageTimeSettings.EnableRosterDuty</c>.</summary>
    public bool IsRosterActive { get; set; }

    /// <summary>Legacy <c>isNotReturnFromLeaveLogicEnabled</c> = <c>ManagePayrollSettings.EnableNotReturnFromLeaveLogic</c>.</summary>
    public bool IsNotReturnFromLeaveLogicEnabled { get; set; }

    /// <summary>Legacy <c>enableOTInTimeEngine</c> = <c>ManageTimeSettings.EnableOvertimeInTimeEngine</c>.</summary>
    public bool EnableOvertimeInTimeEngine { get; set; }

    /// <summary>Legacy <c>countWeekoffOnPH</c>.</summary>
    public bool CountWeekoffOnPH { get; set; }

    /// <summary>Legacy <c>countRestDayOnPH</c>.</summary>
    public bool CountRestDayOnPH { get; set; }

    /// <summary>Legacy <c>fullMonthLeaveAdjForFixedDays</c> = <c>ManagePayrollSettings.FullMonthLeaveAdjForFixedDays</c>.</summary>
    public bool FullMonthLeaveAdjForFixedDays { get; set; }

    /// <summary>Legacy <c>probationDaysCalculation</c> = <c>ManagePersonnelSettings.ProbationDaysCalculation</c>.</summary>
    public ProbationDaysCalculation ProbationDaysCalculation { get; set; }

    /// <summary>Legacy <c>forfeitRule</c> = <c>ManageTimeSettings.ForfeitRule</c>.</summary>
    public ForfeitRule ForfeitRule { get; set; }

    /// <summary>Legacy <c>enableTimeRegisterApproval</c>.</summary>
    public bool EnableTimeRegisterApproval { get; set; }

    /// <summary>Legacy <c>autoApproveTimeLog</c>.</summary>
    public bool AutoApproveTimeLog { get; set; }

    /// <summary>Legacy <c>enableTimeEngineAutorun</c>.</summary>
    public bool EnableTimeEngineAutorun { get; set; }

    /// <summary>Legacy <c>useActualTransInPlannedTimePolicy</c>.</summary>
    public bool UseActualTransInPlannedTimePolicy { get; set; }

    /// <summary>Legacy <c>dedStartMonth</c> = <c>ManageTimeSettings.LeaveDeductionStartMonth</c>.</summary>
    public int LeaveDeductionStartMonth { get; set; }

    /// <summary>Legacy <c>earliestEvaluationDate</c> = <c>ManageTimeSettings.EarliestEvaluationDate</c>.</summary>
    public DateTime EarliestEvaluationDate { get; set; }

    /// <summary>Legacy <c>isHourlyLeaveLogicEnabled</c>. Consumed by T2/T3, carried here for them.</summary>
    public bool IsHourlyLeaveLogicEnabled { get; set; }

    /// <summary>Legacy <c>should_SignIn_AfterHourlyLeave</c>. Consumed by T3.</summary>
    public bool ShouldSignInAfterHourlyLeave { get; set; }

    /// <summary>Legacy <c>should_SignOut_BeforeHourlyLeave</c>. Consumed by T3.</summary>
    public bool ShouldSignOutBeforeHourlyLeave { get; set; }

    /// <summary>Legacy <c>exclude_Paidleaveaccrual_On_Absent</c>. Consumed by T2.</summary>
    public bool ExcludePaidLeaveAccrualOnAbsent { get; set; }

    /// <summary>Legacy <c>alMaxLimitReachedNotification</c> -- <c>SystemNotification</c> row named
    /// <c>ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION</c> is active.</summary>
    public bool AnnualLeaveMaxLimitReachedNotification { get; set; }

    /// <summary>
    /// Legacy <c>enableAuditLogInTimeEngine</c> = <c>ManageSecuritySettings.EnableAuditLogInTimeEngine</c>.
    /// Carried for parity only: the legacy XAF <c>AuditTrailService.BeginSessionAudit</c>/<c>SaveAuditData</c>
    /// plumbing it gated has NO equivalent here (ABP's own auditing is always on via <c>FullAuditedEntity</c>),
    /// so nothing in this port reads it.
    /// </summary>
    public bool EnableAuditLogInTimeEngine { get; set; }
}

/// <summary>
/// Global (non-employee-specific) lookup snapshot loaded once per run, replacing legacy
/// <c>Program.GetInitialVariables</c>'s static lists and "well-known code" id lookups.
/// </summary>
public class TimeEngineMasterData
{
    public List<AbsenceType> AbsenceTypes { get; set; } = new List<AbsenceType>();

    public List<WageType> WageTypes { get; set; } = new List<WageType>();

    public List<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    public List<EmployeeSubGroup> EmployeeSubGroups { get; set; } = new List<EmployeeSubGroup>();

    public List<EmployeeSubGroupAbsence> EmployeeSubGroupAbsences { get; set; } = new List<EmployeeSubGroupAbsence>();

    public List<EmployeeSubGroupGratuity> EmployeeSubGroupGratuities { get; set; } = new List<EmployeeSubGroupGratuity>();

    public List<EmployeeSubGroupPayment> EmployeeSubGroupPayments { get; set; } = new List<EmployeeSubGroupPayment>();

    /// <summary>Legacy <c>AnnualLeave</c> -- <c>AbsenceType.Code == "1"</c>.</summary>
    public AbsenceType AnnualLeave { get; set; }

    /// <summary>Legacy <c>UnpaidLeaveLeave</c> -- <c>AbsenceType.Code == "2"</c>.</summary>
    public AbsenceType UnpaidLeave { get; set; }

    /// <summary>Legacy <c>SickLeaveLeave</c> -- <c>AbsenceType.Code == "3"</c>.</summary>
    public AbsenceType SickLeavePaid { get; set; }

    /// <summary>Legacy <c>SickLeaveUnPaidLeave</c> -- <c>AbsenceType.Code == "5"</c>.</summary>
    public AbsenceType SickLeaveUnpaid { get; set; }

    /// <summary>Legacy <c>AnnualLeaveList</c> -- QuotaType == AnnualLeave OR the Code=="1" row.</summary>
    public List<AbsenceType> AnnualLeaveTypes { get; set; } = new List<AbsenceType>();

    /// <summary>Legacy <c>annualLeaveOid</c>. 0 when no <c>AbsenceType.Code == "1"</c> row exists.</summary>
    public int AnnualLeaveTypeId { get; set; }

    /// <summary>Legacy <c>ticketTypeOid</c> -- <c>TicketType.Code == "1"</c> (the employee's own air ticket).</summary>
    public int AirTicketTypeId { get; set; }

    /// <summary>Legacy <c>timeTransferTypeOid</c> -- <c>TimeTransferType.Code == "1"</c>.</summary>
    public int TimeTransferTypeId { get; set; }

    /// <summary>Legacy <c>clockInOid</c> -- <c>TransType.Code == "1"</c>. Consumed by T3.</summary>
    public int ClockInTransTypeId { get; set; }

    /// <summary>Legacy <c>clockOutOid</c> -- <c>TransType.Code == "2"</c>. Consumed by T3.</summary>
    public int ClockOutTransTypeId { get; set; }

    /// <summary>Legacy <c>extraWorkingAttType</c> -- <c>AttendanceType.Code == "1"</c>. Consumed by T2.</summary>
    public AttendanceType ExtraWorkingAttendanceType { get; set; }

    /// <summary>
    /// Legacy <c>wtBasicPay</c> -- <c>WageType.Code == "1000" &amp;&amp; BillingMethod == CalendarDaysPerMonth</c>.
    /// Only used by the "not returned from leave" AbsentDay branch.
    /// </summary>
    public WageType CalendarDaysBasicPayWageType { get; set; }
}

/// <summary>
/// Flattened punch transaction, replacing the legacy throwaway <c>HCM10.TimeEngine.MyTimeEvent</c> projection
/// class (legacy <c>Program.cs</c> line ~2628). <see cref="TransDateTime"/> is precomputed exactly as legacy
/// did: <c>TransDate.Date + TransTime's hour/minute/second</c>.
/// </summary>
public class TimePunchEvent
{
    public int EmployeeId { get; set; }

    public int TransTypeId { get; set; }

    public string TransTypeCode { get; set; }

    public DateTime TransDate { get; set; }

    public DateTime TransTime { get; set; }

    public DateTime TransDateTime { get; set; }
}

/// <summary>
/// Everything the engine pre-loads ONCE per employee before entering the day-by-day loop (legacy
/// <c>CreateTimeRegister</c>'s "Load All Common Objects" + "Get Quota information" regions, lines ~383-477).
/// Passing this object around replaces the legacy static-class-plus-15-loose-parameters style.
/// </summary>
public class TimeEngineEmployeeContext
{
    public Employee Employee { get; set; }

    /// <summary>Effective (possibly narrowed) run window for THIS employee -- see
    /// <c>TimeEngineService</c>'s contract/joining-date/retro-date resolution.</summary>
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public PayrollManagement.EmployeePayrollStatus PayrollStatus { get; set; }

    /// <summary>See <see cref="TimeEngineRunInput.EmployeeSubGroupIds"/> for why this is caller-supplied.</summary>
    public EmployeeSubGroup EmployeeSubGroup { get; set; }

    public DateTime CutoffDate { get; set; }

    public List<TimeRegister> TimeRegisters { get; set; } = new List<TimeRegister>();

    public List<EmployeeContract> Contracts { get; set; } = new List<EmployeeContract>();

    public List<EmployeeOrgAssignment> OrgAssignments { get; set; } = new List<EmployeeOrgAssignment>();

    public List<EmployeeDutyPeriod> DutyPeriods { get; set; } = new List<EmployeeDutyPeriod>();

    public List<EmployeeTimePolicy> TimePolicies { get; set; } = new List<EmployeeTimePolicy>();

    /// <summary>Null when legacy's <c>loadTimeEvent</c> guard evaluated false (all policies are PlannedTime and
    /// <c>UseActualTransInPlannedTimePolicy</c> is off) -- null vs. empty is meaningful, T3 relies on it.</summary>
    public List<TimePunchEvent> TimeEvents { get; set; }

    public List<EmployeeSubstituteDuty> SubstituteDuties { get; set; } = new List<EmployeeSubstituteDuty>();

    public List<TimeTransfer> TimeTransfers { get; set; } = new List<TimeTransfer>();

    public List<EmployeeAbsence> Absences { get; set; } = new List<EmployeeAbsence>();

    /// <summary>Legacy <c>empAbsenceNotReturnList</c> -- despite the plural name it is a single
    /// <c>FirstOrDefault()</c> row (earliest open, return-date-required, non-hourly absence).</summary>
    public EmployeeAbsence NotReturnedAbsence { get; set; }

    public List<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();

    public List<EmployeeOvertime> Overtimes { get; set; } = new List<EmployeeOvertime>();

    public List<EmployeeTicket> Tickets { get; set; } = new List<EmployeeTicket>();

    public List<EmployeeAbsAdjustment> AbsAdjustments { get; set; } = new List<EmployeeAbsAdjustment>();

    public List<EmployeeAbsenceQuota> AnnualLeaveQuotas { get; set; } = new List<EmployeeAbsenceQuota>();

    public List<EmployeeAbsenceQuota> OtherLeaveQuotas { get; set; } = new List<EmployeeAbsenceQuota>();

    public List<EmployeeTicketQuota> TicketQuotas { get; set; } = new List<EmployeeTicketQuota>();

    public List<EmployeeGratuityQuota> GratuityQuotas { get; set; } = new List<EmployeeGratuityQuota>();

    public List<EmployeeBasicPay> BasicPays { get; set; } = new List<EmployeeBasicPay>();

    /// <summary>
    /// Legacy <c>newLeaveQuota</c> local (legacy <c>Program.cs</c> line ~468). NOTE it is declared OUTSIDE the
    /// day loop and only ever set to <c>true</c>, never back to <c>false</c> -- so once the engine has created
    /// an annual-leave quota for this employee, the flag stays set for every REMAINING day of the employee's
    /// run. That stickiness is load-bearing for the <c>AnnualLeaveAccrualPerDay</c> roll-up subtraction, so it
    /// lives on the per-employee context rather than the per-day one. Preserved exactly.
    /// </summary>
    public bool AnnualLeaveQuotaCreated { get; set; }
}

/// <summary>
/// Snapshot of the four <see cref="TimeRegister"/> values taken BEFORE the day's field calculation, matching
/// legacy's <c>prevAbsentDay</c>/<c>prevActualWorkingDay</c>/<c>prevDedLateInEarlyOutMins</c>/
/// <c>prevOvertimeMinutes</c> locals (legacy <c>Program.cs</c> lines ~1338, 1358, 1364, 1374). Legacy could keep
/// them as locals because the whole day was one giant method body; here the day is split across helpers that
/// each take only the context, so they are carried on <see cref="TimeRegisterDayContext.PreviousValues"/>.
/// </summary>
public class TimeRegisterPreviousValues
{
    public double AbsentDay { get; set; }

    public double ActualWorkingDay { get; set; }

    public double DedLateInEarlyOutMins { get; set; }

    public double OvertimeMinutes { get; set; }
}

/// <summary>
/// Per-day state handed to <see cref="ITimeRegisterFieldCalculator"/> and
/// <see cref="IPunchResolutionStrategy"/>. Replaces the legacy calculators' <c>(IObjectSpace objectSpace,
/// TimeRegister timeRegister, List&lt;X&gt; ..., List&lt;Y&gt; ..., bool ...)</c> parameter lists: legacy had
/// to pass every pre-loaded list explicitly because its calculators were static; here everything lives on one
/// context object and the calculators are DI-managed domain services.
///
/// <para><b>Contract for T2/T3 implementors:</b> <see cref="TimeRegister"/> is the row being built and is
/// mutated in place by the orchestrator between calls, in exactly the legacy order (see
/// <c>TimeEngineService.CalculateDayAsync</c>). Read-your-predecessor's-output is therefore both allowed and
/// required -- e.g. <c>GetAbsentDayAsync</c> legitimately reads <c>TimeRegister.ActualWorkingDay</c>, which the
/// orchestrator has already assigned. Do NOT reorder.</para>
/// </summary>
public class TimeRegisterDayContext
{
    /// <summary>The row being built for <see cref="DayDate"/>. Mutated in place by the orchestrator.</summary>
    public TimeRegister TimeRegister { get; set; }

    public DateTime DayDate { get; set; }

    public Employee Employee { get; set; }

    /// <summary>
    /// Resolved duty for the day. Also assigned to <c>TimeRegister.Duty</c>/<c>DutyId</c>, but exposed here
    /// too because <c>TimeRegister</c>'s navigation property is not guaranteed to be loaded by EF.
    /// </summary>
    public Duty Duty { get; set; }

    public DutyPeriod DutyPeriod { get; set; }

    public TimePolicy TimePolicy { get; set; }

    /// <summary>Legacy <c>IsNotReturnFromLeave</c> local (legacy <c>Program.cs</c> line ~519).</summary>
    public bool IsNotReturnFromLeave { get; set; }

    /// <summary>Legacy <c>isFutureDate</c> local (day is after <see cref="TimeEngineEmployeeContext.CutoffDate"/>).</summary>
    public bool IsFutureDate { get; set; }

    /// <summary>Legacy <c>isNewTimeRegRecord</c> local.</summary>
    public bool IsNewTimeRegisterRecord { get; set; }

    /// <summary>Legacy <c>empSubGroupLeave</c> for the annual-leave type on this day (may be null).</summary>
    public EmployeeSubGroupAbsence AnnualLeaveSubGroupAbsence { get; set; }

    /// <summary>The employee's live annual-leave quota row for this day (may be null).</summary>
    public EmployeeAbsenceQuota AnnualLeaveQuota { get; set; }

    /// <summary>
    /// Pre-calculation snapshot of the fields legacy compared against after the day was computed (approval
    /// status transitions and the <c>EarliestMasterDataChange</c> retro trigger). Filled by the orchestrator
    /// immediately before the field calculation runs; T2/T3 implementors must not write to it.
    /// </summary>
    public TimeRegisterPreviousValues PreviousValues { get; set; } = new TimeRegisterPreviousValues();

    public TimeEngineEmployeeContext EmployeeContext { get; set; }

    public TimeEngineSettings Settings { get; set; }

    public TimeEngineMasterData MasterData { get; set; }
}
