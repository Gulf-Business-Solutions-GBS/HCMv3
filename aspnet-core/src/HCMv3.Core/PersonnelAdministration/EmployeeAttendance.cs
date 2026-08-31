using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee attendance transaction record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAttendance</c>, Wave 1 deferred bucket).
///
/// The legacy cascade side effects (auto-accruing into a linked <see cref="EmployeeAbsenceQuota"/> when
/// <c>AttendanceType.HaveQuotaAccrual</c> is set, hourly-based duty/roster lookups to derive
/// <see cref="TotalAttendDays"/> from hours) reference the (already-ported) TimeManagement duty/roster
/// infrastructure and are ported at the app-service level in <c>EmployeeAttendancesAppService</c> in a
/// simplified form, not on the entity. The legacy <c>prevXxx</c> change-tracking fields are
/// <c>[NonPersistent]</c> and not re-ported as columns.
/// </summary>
public class EmployeeAttendance : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AttendanceTypeId { get; set; }

    public virtual AttendanceType AttendanceType { get; set; }

    public virtual AttendanceUnit AttendanceUnit { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual DateTime StartTime { get; set; }

    public virtual DateTime EndTime { get; set; }

    public virtual double AttendanceHours { get; set; }

    /// <summary>Total number of attendance days accrued for this record.</summary>
    public virtual double TotalAttendDays { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this row, when it was raised through self service.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null when entered directly by HR rather than requested. Read by the ApprovedOnly filter, which
    /// is what keeps a pending row out of payroll and reports.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
