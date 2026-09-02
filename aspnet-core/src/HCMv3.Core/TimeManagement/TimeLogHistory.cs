using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// What a day read, raw and approved, at the moment a recalculation invalidated an approval (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.Time.TimeLogHistory</c>, lines 11-20).
///
/// <para>
/// Written by the time engine, never by the approval screen. When recalculation changes an already-approved
/// day, the approval it was granted no longer describes reality: the engine records both triples here, sets
/// the day back to <see cref="TimeRegisterStatus.Reopened"/> and clears
/// <see cref="TimeRegister.TimeLogApprovalId"/> so the day becomes eligible for a fresh request. Without this
/// row the fact that somebody had approved a different set of figures would simply be overwritten -- and
/// without the clear, an approved day could never be corrected again.
/// </para>
/// </summary>
public class TimeLogHistory : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>The approval that is being invalidated.</summary>
    public virtual int TimeLogApprovalId { get; set; }

    public virtual TimeLogApproval TimeLogApproval { get; set; }

    public virtual int TimeRegisterId { get; set; }

    public virtual TimeRegister TimeRegister { get; set; }

    public virtual DateTime DateOfAction { get; set; }

    // The RAW (engine-calculated) triple as it stood before the recalculation.

    public virtual double ActualWorkingDay { get; set; }

    public virtual double AbsentDay { get; set; }

    public virtual double DedLateInEarlyOutMins { get; set; }

    // The APPROVED triple that is being thrown away.

    public virtual double ApprovedActualWorkingDay { get; set; }

    public virtual double ApprovedAbsentDay { get; set; }

    public virtual double ApprovedDedLateInEarlyOutMins { get; set; }
}
