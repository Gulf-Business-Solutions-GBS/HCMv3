using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Calculated per-employee-per-day time result (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.TimeRegister</c>, Wave 4). Widest entity in the module
/// (60+ scalar fields), but the legacy source contains NO calculation logic itself -- the day-by-day
/// calculation engine that populates these fields lives elsewhere and is out of scope for this port. Ported
/// here as a faithful data-storage shape; only basic required-field + (Employee, DayDate) uniqueness
/// validation is enforced in <c>TimeRegistersAppService</c> (no calculation logic to replicate, since none
/// exists in the legacy source).
///
/// NOT PORTED (deliberate simplifications):
///  - <c>WfRequest</c> FK to <c>WorkflowRequest</c> -- NO LONGER TRUE. Now ported as
///    <see cref="TimeLogApprovalId"/>, pointing at the <see cref="TimeLogApproval"/> batch rather than at the
///    request directly (the request is reachable from the batch). See that property for why the reopen path
///    needs it.
///  - The legacy <c>OnDeleting</c> logic that reverses <c>AnnualLeaveAccrualPerDay</c> from the matching
///    <c>EmployeeAbsenceQuota.QuotaAccrued</c> row -- <c>EmployeeAbsenceQuota</c> does not exist in this
///    port (LeaveManagement module not yet ported).
///  - <c>LeaveType</c> (legacy FK to <c>AbsenceType</c>) and <c>AttendanceType</c> (legacy FK to
///    <c>AttendanceType</c>) reference entities that belong to modules not yet ported in this solution
///    (LeaveManagement). Both are kept as bare nullable <c>int</c> id columns (<see cref="LeaveTypeId"/>,
///    <see cref="AttendanceTypeId"/>) with NO navigation property and NO FK constraint, so the shape is
///    preserved for a future wave to wire up without a schema change.
/// </summary>
public class TimeRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// Legacy <c>[Indexed("DayDate", Unique = true)]</c> on Employee -- i.e. a unique (Employee, DayDate)
    /// pair. Enforced both here (DB unique index, wired by the DbContext owner) and in the app service.
    /// </summary>
    public virtual DateTime DayDate { get; set; }

    public virtual int? DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    public virtual int? DutyId { get; set; }

    public virtual Duty Duty { get; set; }

    public virtual int? TimePolicyId { get; set; }

    public virtual TimePolicy TimePolicy { get; set; }

    // Shift timings.
    public virtual DateTime ShiftStartTime1 { get; set; }

    public virtual DateTime BreakStartTime { get; set; }

    public virtual DateTime BreakEndTime { get; set; }

    public virtual DateTime ShiftEndTime1 { get; set; }

    // Actual timings from punch in/out, as considered for calculation.
    public virtual DateTime ActualIn { get; set; }

    public virtual DateTime BreakOut { get; set; }

    public virtual DateTime BreakIn { get; set; }

    public virtual DateTime ActualOut { get; set; }

    // Values after comparing shift start/end with actual punch in/out.
    public virtual DateTime ActualStartTime1 { get; set; }

    public virtual DateTime ActualBreakStartTime { get; set; }

    public virtual DateTime ActualBreakEndTime { get; set; }

    public virtual DateTime ActualEndTime1 { get; set; }

    // Planned/actual/total working & absence minutes.
    public virtual double PlannedWorkingMins { get; set; } // TimeType = 21

    public virtual double ActualWorkingMins { get; set; } // TimeType = 22

    public virtual double TotalWorkingMins { get; set; } // TimeType = 22

    public virtual double ActualAbsenceMins { get; set; } // TimeType = 23

    public virtual double LeaveDay { get; set; }

    /// <summary>
    /// Legacy FK to (not-yet-ported) <c>AbsenceType</c> -- see class remarks. Bare id, no navigation/FK.
    /// </summary>
    public virtual int? LeaveTypeId { get; set; }

    public virtual double LeaveMinutes { get; set; }

    // Leave breakdown.
    public virtual double AnnualLeaveDay { get; set; } // TimeType = 1

    public virtual double UnpaidLeaveDay { get; set; } // TimeType = 2

    public virtual double SickLeavePaidDay { get; set; } // TimeType = 3

    public virtual double SickLeaveUnpaidDay { get; set; } // TimeType = 5

    public virtual double PublicHoliday { get; set; }

    public virtual double WeeklyOff { get; set; } // TimeType = 8

    public virtual double RestDay { get; set; } // TimeType = 9

    // Accrual fields.
    public virtual double AnnualLeaveAccrualPerDay { get; set; } // TimeType = 10

    public virtual double TicketAccrualPerDay { get; set; } // TimeType = 13

    public virtual double AirTicketPaidDay { get; set; } // TimeType = 16

    public virtual double ActiveDay { get; set; }

    public virtual double InactiveDay { get; set; }

    public virtual double OvertimeMinutes { get; set; }

    public virtual int? OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }

    public virtual double PlannedWorkingDay { get; set; } // TimeType = 28

    public virtual double ActualWorkingDay { get; set; } // TimeType = 29

    public virtual double AttendanceDay { get; set; }

    /// <summary>
    /// Legacy FK to (not-yet-ported) <c>AttendanceType</c> -- see class remarks. Bare id, no navigation/FK.
    /// </summary>
    public virtual int? AttendanceTypeId { get; set; }

    public virtual double AttendanceMinutes { get; set; }

    public virtual double AbsentDay { get; set; } // TimeType = 33

    public virtual double LateIn { get; set; }

    public virtual double DeductionLateIn { get; set; }

    public virtual double EarlyOut { get; set; }

    public virtual double DeductionEarlyOut { get; set; }

    // Break-prefixed equivalents.
    public virtual double BreakEarlyOut { get; set; }

    public virtual double BreakLateIn { get; set; }

    public virtual double BreakDeductionEarlyOut { get; set; }

    public virtual double BreakDeductionLateIn { get; set; }

    public virtual double DedLateInEarlyOutMins { get; set; }

    public virtual double DedLateInEarlyOutDay { get; set; }

    public virtual double DedAfterMonthlyTol { get; set; }

    /// <summary>
    /// Whether to include this day in leave-pay calculation (set when RestDay = 1 OR WeeklyOff = 1 and
    /// WageType.ProrateMethod = ActiveDays -- Payroll module not ported, so this is a plain flag here).
    /// </summary>
    public virtual double IncludeInLeavePay { get; set; }

    public virtual double LeaveAdjDay { get; set; }

    // Time register approval dashboard.
    public virtual double ApprovedActualWorkingDay { get; set; }

    public virtual double ApprovedAbsentDay { get; set; }

    public virtual double ApprovedDedLateInEarlyOutMins { get; set; }

    public virtual TimeRegisterStatus Status { get; set; }

    /// <summary>
    /// The <see cref="TimeLogApproval"/> whose approval produced the <c>Approved*</c> figures above, or
    /// <c>null</c> when nothing has approved this day (or when an approval has since been invalidated by a
    /// recalculation).
    ///
    /// <para>
    /// Replaces legacy's <c>WfRequest</c> FK, and it exists for one reason: the reopen path. When the engine
    /// recalculates a day and the raw figures move, it needs to find the approval that no longer describes
    /// the day, write a <see cref="TimeLogHistory"/> row and CLEAR this column -- without which an approved
    /// day could never be corrected again, because it would never become eligible for a new request. Bare id
    /// with no navigation property on purpose: <c>TimeLogApproval -&gt; TimeLogApprovalDetail -&gt;
    /// TimeRegister</c> already exists, and a navigation back would close that loop for no gain.
    /// </para>
    /// </summary>
    public virtual int? TimeLogApprovalId { get; set; }

    [StringLength(1000)]
    public virtual string Remarks { get; set; }
}
