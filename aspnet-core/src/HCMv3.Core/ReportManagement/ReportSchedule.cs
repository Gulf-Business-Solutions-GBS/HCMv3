using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// A saved report distribution schedule: WHAT report to run, WHICH slice of it each output covers,
/// WHO receives that slice, WHERE it is delivered and HOW OFTEN.
///
/// This has no legacy XAF counterpart -- HCM10 had no report scheduling. The screen design follows the
/// four-step wizard the customer specified (Definition, Filter, Destination, Occurrence).
///
/// THE SECURITY PROPERTY THIS TYPE EXISTS TO GUARANTEE. A schedule is authored once, by someone who can
/// usually see far more data than its recipients (an HR admin), but it runs unattended and delivers to
/// many people. Rows are therefore NOT filtered by the author's entitlement. Each slice is filtered by
/// the RECIPIENT's own row-level-security scope, re-resolved at run time through
/// <c>SystemSecurity.Services.IRlsService</c>. A department head cannot receive rows outside their scope
/// even if the schedule's author could see them, and the guarantee survives staff moving between
/// departments after the schedule was saved.
///
/// That is also why <see cref="ReportScheduleRecipient"/> stores RULES rather than e-mail addresses.
/// </summary>
public class ReportSchedule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string Name { get; set; }

    /// <summary>Grid report or print report. Decides how <see cref="ReportKey"/> is interpreted.</summary>
    public virtual ScheduledReportKind ReportKind { get; set; }

    /// <summary>
    /// Identifies the report within its family: the grid report's app-service key, or the print
    /// report's <c>PrintReport.ReportName</c>. Deliberately a string rather than an FK -- grid reports
    /// are code, not rows, so there is no table to point at.
    /// </summary>
    [Required]
    [StringLength(256)]
    public virtual string ReportKey { get; set; }

    public virtual ScheduledReportFormat Format { get; set; }

    /// <summary>Comma-separated addresses notified when a run starts/finishes. Not the report recipients.</summary>
    [StringLength(2048)]
    public virtual string NotificationEmails { get; set; }

    public virtual bool NotifyOnStart { get; set; }

    public virtual bool NotifyOnCompletion { get; set; }

    /// <summary>Inactive schedules keep their definition and history but are not fired by the scheduler.</summary>
    public virtual bool IsActive { get; set; } = true;

    // ---- burst -----------------------------------------------------------------------------------

    public virtual ReportBurstDimension BurstDimension { get; set; }

    /// <summary>
    /// Only for <see cref="ReportBurstDimension.OrganizationUnit"/>: which level the burst happens at,
    /// as an <c>OrganizationUnitType</c> id. Bursting "by department" and "by division" are the same
    /// mechanism at different levels.
    /// </summary>
    public virtual int? BurstOrganizationUnitTypeId { get; set; }

    /// <summary>
    /// Only for <see cref="ScheduledReportKind.PrintReport"/>: the report parameter the burst key is
    /// injected into. Print reports each have bespoke parameters, so this cannot be inferred -- see the
    /// remarks on <see cref="ReportScheduleFilter"/>.
    /// </summary>
    [StringLength(128)]
    public virtual string BurstParameterName { get; set; }

    // ---- occurrence ------------------------------------------------------------------------------

    public virtual ReportScheduleOccurrenceType OccurrenceType { get; set; }

    public virtual ReportRecurringPattern? RecurringPattern { get; set; }

    public virtual DateTime FirstOccurrence { get; set; }

    /// <summary>Null means "no end date". Ignored for <see cref="ReportScheduleOccurrenceType.Once"/>.</summary>
    public virtual DateTime? EndingOn { get; set; }

    /// <summary>Set by the scheduler after each fire; null until the first run.</summary>
    public virtual DateTime? LastRunTime { get; set; }

    public virtual DateTime? NextRunTime { get; set; }

    // ---- children --------------------------------------------------------------------------------

    public virtual ICollection<ReportScheduleFilter> Filters { get; set; } = new List<ReportScheduleFilter>();

    public virtual ICollection<ReportScheduleRecipient> Recipients { get; set; } = new List<ReportScheduleRecipient>();

    public virtual ICollection<ReportScheduleDestination> Destinations { get; set; } = new List<ReportScheduleDestination>();
}
