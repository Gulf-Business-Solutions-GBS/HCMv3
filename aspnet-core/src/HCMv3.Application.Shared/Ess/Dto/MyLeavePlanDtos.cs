using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One of the employee's own leave plans, as the self-service list shows it. Columns follow the legacy ESS
/// list (<c>Views/LeavePlanner/Index.cshtml</c>): request number, status, start, end, planned days, created.
/// </summary>
public class MyLeavePlanDto : MyRequestDtoBase
{
    /// <summary>The request's number once it has been submitted; null for a draft or a plan HR recorded.</summary>
    public string RequestNumber { get; set; }

    /// <summary>An approver sent it back: Draft again, editable, and resubmitted under the same request.</summary>
    public bool IsSentBack { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Calendar days, start and end included (legacy "Planned Calendar Days").</summary>
    public double PlannedDays { get; set; }

    /// <summary>Annual leave balance projected to the start date, as taken when the plan was last saved.</summary>
    public double? ProjectedLeaveBalance { get; set; }

    public string Comments { get; set; }

    public DateTime CreationTime { get; set; }
}

/// <summary>Plans a leave and, when the type is workflow-enabled, sends it for approval in the same call.</summary>
public class RequestLeavePlanInput
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

/// <summary>
/// A leave plan filed as a draft rather than submitted. Every field optional: a draft is what is saved because
/// it cannot be finished yet. The date rules are checked at submit.
/// </summary>
public class SaveLeavePlanDraftInput
{
    /// <summary>Null to file a new draft; the draft's own id to change one already filed or sent back.</summary>
    public int? Id { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

/// <summary>The dates the form holds while the employee is still choosing them.</summary>
public class PreviewLeavePlanInput
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

/// <summary>
/// The figures beside the leave plan form (its "Leave Info"), re-asked whenever a date changes -- legacy
/// <c>LeavePlannerRequestForm.js</c> <c>OnLeaveDateChange</c>.
/// </summary>
public class LeavePlanPreviewDto
{
    /// <summary>Calendar days, start and end included.</summary>
    public double PlannedDays { get; set; }

    /// <summary>Annual leave balance projected to the start date. Null when there is no start date yet, or no
    /// annual leave type is configured.</summary>
    public double? ProjectedLeaveBalance { get; set; }

    /// <summary>
    /// What those dates would charge as annual leave -- calendar days less the week-offs and holidays the type
    /// excludes. Information only; a plan books nothing.
    /// </summary>
    public double? LeaveDays { get; set; }
}
