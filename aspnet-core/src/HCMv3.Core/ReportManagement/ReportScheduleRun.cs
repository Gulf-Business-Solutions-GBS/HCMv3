using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One execution of a <see cref="ReportSchedule"/>.
///
/// This exists because report distribution fails silently by nature: nobody notices an e-mail that was
/// never sent. Without a run log the only evidence a schedule misfired is a recipient eventually
/// complaining, and by then the period is gone. Every run and every slice is recorded whether it
/// succeeded, failed, or produced nothing.
/// </summary>
public class ReportScheduleRun : FullAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportScheduleId { get; set; }

    public virtual ReportSchedule ReportSchedule { get; set; }

    public virtual DateTime StartedAt { get; set; }

    public virtual DateTime? FinishedAt { get; set; }

    public virtual ReportRunStatus Status { get; set; }

    /// <summary>True when a person pressed Run rather than the scheduler firing it.</summary>
    public virtual bool IsManual { get; set; }

    /// <summary>Number of burst slices the run resolved. Zero is itself a finding worth seeing.</summary>
    public virtual int SliceCount { get; set; }

    [StringLength(4000)]
    public virtual string ErrorMessage { get; set; }

    public virtual ICollection<ReportScheduleRunSlice> Slices { get; set; } = new List<ReportScheduleRunSlice>();
}

/// <summary>
/// One burst slice within a run: one rendered output, for one burst key, delivered to one recipient.
///
/// Deliberately one row PER RECIPIENT rather than per slice. If a department has three recipients and
/// one address bounces, that must be visible as one failure against two successes -- not as a single
/// ambiguous "partially delivered" row that nobody can act on.
/// </summary>
public class ReportScheduleRunSlice : FullAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long ReportScheduleRunId { get; set; }

    public virtual ReportScheduleRun ReportScheduleRun { get; set; }

    /// <summary>The burst key this output covers -- an org unit id, manager employee id, sponsor id, etc.</summary>
    [StringLength(128)]
    public virtual string BurstKey { get; set; }

    /// <summary>Human-readable form of <see cref="BurstKey"/>, captured at run time so history stays readable
    /// after the underlying unit is renamed or deleted.</summary>
    [StringLength(512)]
    public virtual string BurstKeyDisplay { get; set; }

    /// <summary>Resolved recipient. Null for a non-user <see cref="ReportRecipientRule.Explicit"/> address.</summary>
    public virtual long? RecipientUserId { get; set; }

    [StringLength(256)]
    public virtual string RecipientEmail { get; set; }

    public virtual ReportDestinationMethod Method { get; set; }

    public virtual ReportRunStatus Status { get; set; }

    /// <summary>Rows in this recipient's slice AFTER their row-level-security scope was applied.</summary>
    public virtual int RowCount { get; set; }

    /// <summary>
    /// Whether this slice was scoped to the recipient's own entitlement. Recorded per slice, not just on
    /// the recipient rule, so an auditor can answer "was this file scoped?" from the run log alone
    /// without reconstructing what the schedule looked like at the time.
    /// </summary>
    public virtual bool WasRecipientScoped { get; set; }

    [StringLength(512)]
    public virtual string FileName { get; set; }

    [StringLength(4000)]
    public virtual string ErrorMessage { get; set; }
}
