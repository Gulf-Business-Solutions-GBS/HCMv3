using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// The SHARED per-day field calculator for the Time Engine (to be ported in wave T2 from legacy
/// <c>HCM10.TimeEngine.GeneralTimeService</c>, a 1,197-line static class).
///
/// <para><b>Why one shared calculator and not five parallel strategies.</b> Legacy
/// <c>Program.CreateTimeRegister</c> calls <c>GeneralTimeService.*</c> UNCONDITIONALLY for every field
/// (legacy <c>Program.cs</c> lines ~1185-1390), regardless of the employee's
/// <see cref="TimePolicy.TimeMgmtStatus"/>. Only punch resolution dispatches by policy (see
/// <see cref="IPunchResolutionStrategy"/>), plus three narrow FlexiTime overrides (see
/// <see cref="IFlexiTimeCalculationOverride"/>). This interface therefore mirrors GeneralTimeService 1:1.</para>
///
/// <para><b>Parameter-list collapse.</b> Every legacy method took
/// <c>(IObjectSpace, TimeRegister, plus 0-3 pre-loaded lists)</c>. All of those lists now live on
/// <see cref="TimeRegisterDayContext"/> / <see cref="TimeEngineEmployeeContext"/>, so every method here takes
/// exactly one parameter. The legacy method each one maps to is named in its doc comment.</para>
///
/// <para><b>Ordering contract.</b> The orchestrator calls these in the exact legacy order and assigns each
/// result onto <see cref="TimeRegisterDayContext.TimeRegister"/> before the next call, because several legacy
/// methods read fields written by earlier ones. See <see cref="TimeRegisterDayContext"/> remarks.</para>
///
/// <para><b>ActiveDay gating is the orchestrator's job, not the calculator's.</b> Legacy wraps nearly every
/// call in <c>timeRegister.ActiveDay == 1 ? GeneralTimeService.X(...) : 0</c>. That ternary stays in
/// <c>TimeEngineService</c>; implementations here must NOT re-check ActiveDay (they will not be called when it
/// is 0).</para>
/// </summary>
public interface ITimeRegisterFieldCalculator : IDomainService
{
    /// <summary>Legacy <c>GeneralTimeService.GetActiveDay(objectSpace, timeRegister, empContractList)</c>.
    /// Contracts come from <see cref="TimeEngineEmployeeContext.Contracts"/>.</summary>
    Task<double> GetActiveDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetPublicHoliday</c> (legacy returns <c>int</c>; widened to
    /// <c>double</c> to match <see cref="TimeRegister.PublicHoliday"/>).</summary>
    Task<double> GetPublicHolidayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetPlannedWorkingMinutes</c>.</summary>
    Task<double> GetPlannedWorkingMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetPlannedWorkingDay</c>.</summary>
    Task<double> GetPlannedWorkingDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAnnualPaidLeaveDay(objectSpace, timeRegister, AnnualLeave, empAbsenceList)</c>.
    /// The AnnualLeave AbsenceType is <see cref="TimeEngineMasterData.AnnualLeave"/>.</summary>
    Task<double> GetAnnualPaidLeaveDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetUnPaidLeaveDay(..., UnpaidLeaveLeave, empAbsenceList)</c>.</summary>
    Task<double> GetUnPaidLeaveDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetSickLeavePaidDay(..., SickLeaveLeave, empAbsenceList)</c>.</summary>
    Task<double> GetSickLeavePaidDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetSickLeaveUnPaidDay(..., SickLeaveUnPaidLeave, empAbsenceList)</c>.</summary>
    Task<double> GetSickLeaveUnPaidDayAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetLeaveType</c>. Legacy returns the <c>AbsenceType</c> entity; this port
    /// returns its id because <see cref="TimeRegister.LeaveTypeId"/> is a bare nullable int with no navigation
    /// property (AbsenceType belongs to a module wired up separately -- see <see cref="TimeRegister"/> remarks).
    /// </summary>
    Task<int?> GetLeaveTypeIdAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetLeaveDay</c>.</summary>
    Task<double> GetLeaveDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetLeaveMinutes</c>.</summary>
    Task<double> GetLeaveMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetIncludeInLeaveDay</c> (assigned to
    /// <see cref="TimeRegister.IncludeInLeavePay"/>).</summary>
    Task<double> GetIncludeInLeaveDayAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>GeneralTimeService.CalculateAutoAttendance(objectSpace, timeRegister, empAttendanceList,
    /// empAbsenceList, empAnnualLeaveQuota)</c>. A <c>void</c> side-effecting method in legacy (it creates/
    /// updates <c>EmployeeAttendance</c> rows); implementations here own their own persistence.
    /// Only invoked when <see cref="TimeEngineSettings.IsAutoAttendanceEnabled"/> is true.
    /// </summary>
    Task CalculateAutoAttendanceAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAttendanceDay</c>.</summary>
    Task<double> GetAttendanceDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAttendanceType</c>. Returns the id rather than the entity, for
    /// the same reason as <see cref="GetLeaveTypeIdAsync"/> (<see cref="TimeRegister.AttendanceTypeId"/>).</summary>
    Task<int?> GetAttendanceTypeIdAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAttendanceMinutes</c>.</summary>
    Task<double> GetAttendanceMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetLateIn(objectSpace, timeRegister, empAbsenceList)</c>.</summary>
    Task<double> GetLateInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetDeductionLateIn</c>.</summary>
    Task<double> GetDeductionLateInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetEarlyOut(objectSpace, timeRegister, empAbsenceList)</c>.</summary>
    Task<double> GetEarlyOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetDeductionEarlyOut</c>.</summary>
    Task<double> GetDeductionEarlyOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetBreakEarlyOut</c>.</summary>
    Task<double> GetBreakEarlyOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetBreakDeductionEarlyOut</c>.</summary>
    Task<double> GetBreakDeductionEarlyOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetBreakLateIn</c>.</summary>
    Task<double> GetBreakLateInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetBreakDeductionLateIn</c>.</summary>
    Task<double> GetBreakDeductionLateInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetDedLateInEarlyOutMinutes</c>.</summary>
    Task<double> GetDedLateInEarlyOutMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetDedLateInEarlyOutDay</c>.</summary>
    Task<double> GetDedLateInEarlyOutDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetTotalWorkingMinutes</c>. NOT called when the day's policy is
    /// FlexiTime -- see <see cref="IFlexiTimeCalculationOverride.GetTotalWorkingMinutesAsync"/>.</summary>
    Task<double> GetTotalWorkingMinutesAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetActualAbsenceMinutes(objectSpace, timeRegister, empAbsenceList,
    /// IsNotReturnFromLeave)</c>. The <c>IsNotReturnFromLeave</c> flag is
    /// <see cref="TimeRegisterDayContext.IsNotReturnFromLeave"/>. NOT called when the day's policy is FlexiTime.
    /// </summary>
    Task<double> GetActualAbsenceMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetActualWorkingMinutes(objectSpace, timeRegister, empAbsAdjustmentList)</c>.</summary>
    Task<double> GetActualWorkingMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetActualWorkingDay</c>.</summary>
    Task<double> GetActualWorkingDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAbsentDay</c>.</summary>
    Task<double> GetAbsentDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetOvertimeMinutes</c>. NOT called when the day's policy is
    /// FlexiTime -- see <see cref="IFlexiTimeCalculationOverride.GetOvertimeMinutesAsync"/>.</summary>
    Task<double> GetOvertimeMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetOvertimeType</c>. Returns the id;
    /// <see cref="TimeRegister.OvertimeTypeId"/> does have a navigation property here, but the id keeps this
    /// contract uniform with the other two type-returning methods and avoids cross-context entity attachment.</summary>
    Task<int?> GetOvertimeTypeIdAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>GeneralTimeService.CalculateOvertime(objectSpace, timeRegister, empOvertimeList, empSubGroup)</c>.
    /// Side-effecting (<c>void</c>) in legacy -- creates/updates <c>EmployeeOvertime</c> rows; implementations
    /// own their persistence. Only invoked when <see cref="TimeEngineSettings.EnableOvertimeInTimeEngine"/> is true.
    /// </summary>
    Task CalculateOvertimeAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAnnualLeaveAccrualPerDay(objectSpace, timeRegister,
    /// empSubGroupLeave?.TotalDays ?? 0, empTimeTransfer)</c>. Both inputs are on the context
    /// (<see cref="TimeRegisterDayContext.AnnualLeaveSubGroupAbsence"/>,
    /// <see cref="TimeEngineEmployeeContext.TimeTransfers"/>).
    /// </summary>
    Task<double> GetAnnualLeaveAccrualPerDayAsync(TimeRegisterDayContext context);

    // --------------------------------------------------------------------------------------------------
    // Below: public on legacy GeneralTimeService but NOT reached from CreateTimeRegister in the current
    // legacy source (GetTicketAccrualPerDay/GetAirTicketPaidDay are called only from a commented-out block,
    // lines ~1428-1450; GetAnnualLeaveType/GetTotalLeaveDayCount/GetActualAttendanceMinutes are internal
    // helpers used by other legacy modules). Declared here so T2 has a home for them rather than inventing a
    // second interface; the orchestrator never calls them.
    // --------------------------------------------------------------------------------------------------

    /// <summary>Legacy <c>GeneralTimeService.GetAnnualLeaveType</c>. Not called by the orchestrator.</summary>
    Task<int?> GetAnnualLeaveTypeIdAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetTotalLeaveDayCount</c>. Not called by the orchestrator.</summary>
    Task<double> GetTotalLeaveDayCountAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetActualAttendanceMinutes</c>. Not called by the orchestrator.</summary>
    Task<double> GetActualAttendanceMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetTicketAccrualPerDay</c>. Not called by the orchestrator
    /// (legacy call site is commented out).</summary>
    Task<double> GetTicketAccrualPerDayAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>GeneralTimeService.GetAirTicketPaidDay</c>. Not called by the orchestrator
    /// (legacy call site is commented out).</summary>
    Task<double> GetAirTicketPaidDayAsync(TimeRegisterDayContext context);
}
