using System.Collections.Generic;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// What the people on one step of an appraisal's approval chain may do -- the legacy "Appraisal WF Setup"
/// row (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.PARequest</c>, one row
/// per request master and step).
///
/// <para>
/// <b>An extension table, not columns on <see cref="RequestDetail"/>.</b> The general workflow setup serves
/// some fifty request types and only the 360 appraisal reads any of this. Putting the flags on the step row
/// itself would show eleven appraisal switches on every leave and loan chain. The owner accepted the
/// separate table (design note batch 8 round B, section 1, question 1).
/// </para>
///
/// <para>
/// <b>Named for what it is rather than for its legacy class.</b> Legacy called it <c>PARequest</c>, which in
/// this codebase would read as one of the duplicate "*Request" tables the workflow design removed. It is not
/// one -- it holds no transaction -- so it is named as the setting it is. The admin screen keeps the legacy
/// title, "Appraisal WF Setup".
/// </para>
///
/// <para>
/// <b>Every flag defaults to off and nothing is seeded.</b> A wrong default silently adds or drops a
/// reviewer, and a reviewer moves the score. A tenant with no row for a step therefore gets legacy's
/// "no PARequest" behaviour: nobody is added, and the review buttons stay hidden.
/// </para>
/// </summary>
public class AppraisalStepSetting : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>The approval chain this row belongs to (legacy <c>RequestMaster</c>).</summary>
    public virtual int RequestMasterId { get; set; }

    public virtual RequestMaster RequestMaster { get; set; }

    /// <summary>The chain step, matching <see cref="RequestDetail.Step"/>. Must be a step the chain defines.</summary>
    public virtual int Step { get; set; }

    /// <summary>The approver on this step may use Copy KPI.</summary>
    public virtual bool CanAddKpi { get; set; }

    /// <summary>Peers may be added on this step, the employee's own nominations capped at <see cref="AllowedPeerCount"/>.</summary>
    public virtual bool CanAddPeer { get; set; }

    /// <summary>The approver on this step is added as a reviewer and may enter ratings.</summary>
    public virtual bool CanAddRatings { get; set; }

    /// <summary>Peers' rating lines are visible to the approver.</summary>
    public virtual bool CanViewPeerRatings { get; set; }

    /// <summary>Non-peer reviewers' rating lines are visible to the approver.</summary>
    public virtual bool CanViewRatings { get; set; }

    /// <summary>A reviewer (peer) sees the rating section and may rate.</summary>
    public virtual bool CanPeerAddRatings { get; set; }

    /// <summary>The overall, peer and employee averages are shown to the approver.</summary>
    public virtual bool CanViewOverallAverageRating { get; set; }

    /// <summary>The employee is added as a reviewer of their own appraisal when approvers are assigned here.</summary>
    public virtual bool CanEmployeeSelfRate { get; set; }

    /// <summary>The employee's current manager is added as a reviewer (as a peer) when approvers are assigned here.</summary>
    public virtual bool AutoAddManager { get; set; }

    /// <summary>
    /// Everyone related to the employee today through one of <see cref="Relationships"/> is added as a
    /// reviewer (as a peer) when approvers are assigned here.
    /// </summary>
    public virtual bool AutoAddRelationShip { get; set; }

    /// <summary>When the last pending reviewer submits, the step is approved automatically as its first approver.</summary>
    public virtual bool AutoSubmitOnLastFill { get; set; }

    /// <summary>How many peers the employee may nominate personally. Peers added by somebody else do not count.</summary>
    public virtual int AllowedPeerCount { get; set; }

    /// <summary>The relationship types <see cref="AutoAddRelationShip"/> reads (legacy <c>PARequestRelationship</c>).</summary>
    public virtual ICollection<AppraisalStepSettingRelationship> Relationships { get; set; }
}
