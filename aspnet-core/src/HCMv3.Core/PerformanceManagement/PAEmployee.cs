using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// 360-style appraisal aggregate root, the newer appraisal model (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAEmployee</c>).
///
/// <para><b>Where the approval routing went.</b> In legacy, <c>PAEmployee</c> derives from
/// <c>Workflow.WorkflowRequest</c>, which supplied <c>Employee</c>/<c>RequestNumber</c>/
/// <c>RequestStatus</c>/<c>WorkflowStep</c> and the whole approval orchestration. HCMv3 inverts that: the
/// appraisal is a plain <see cref="FullAuditedEntity"/> with its own <see cref="EmployeeId"/> FK, and it
/// takes part in approval by implementing <see cref="IWorkflowEnabledEntity"/> -- two nullable columns
/// pointing at the <c>WorkflowRequest</c> that governs it. There is deliberately no separate appraisal
/// *request* table: this one row is both the request and the transaction, so an appraisal cannot be
/// approved in one place and missing in another. The rules live in
/// <c>PerformanceManagement.Workflow.AppraisalWorkflowTransactionHandler</c>.</para>
///
/// Aggregated children -- <see cref="PAEmployeeDetail"/>, <see cref="PAEmployeeReviewer"/>,
/// <see cref="PAEmployeeRating"/> -- are flat satellite entities carrying their own <c>PAEmployeeId</c> FK.
/// </summary>
public class PAEmployee : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy property name is <c>Period</c>; FKs <see cref="PACycle"/>.</summary>
    public virtual int PACycleId { get; set; }

    public virtual PACycle PACycle { get; set; }

    public virtual int FormTypeId { get; set; }

    public virtual FormType FormType { get; set; }

    public virtual int? OverallAverageRatingId { get; set; }

    public virtual PARating OverallAverageRating { get; set; }

    public virtual double OverallAverageScore { get; set; }

    public virtual int? PeerAverageRatingId { get; set; }

    public virtual PARating PeerAverageRating { get; set; }

    public virtual double PeerAverageScore { get; set; }

    public virtual int? EmployeeAverageRatingId { get; set; }

    public virtual PARating EmployeeAverageRating { get; set; }

    public virtual double EmployeeAverageScore { get; set; }

    /// <summary>
    /// LEGACY -- READ ONLY. Retained so appraisals migrated from HRMS keep the text their approvers typed;
    /// nothing in HCMv3 writes it any more.
    ///
    /// <para>
    /// In legacy this was the box an approver typed into before acting, which <c>WorkFlowServices</c> then
    /// concatenated -- with their name and a timestamp -- onto <see cref="WorkflowComments"/> and blanked
    /// again. New comments go to <see cref="WorkflowComment"/> instead, which stores one comment per row
    /// against the request, attributed to an employee rather than to a name string. Dropping the column
    /// would destroy the only copy of what was said on appraisals that predate the workflow engine, so it
    /// stays and is simply never assigned.
    /// </para>
    /// </summary>
    [StringLength(1000)]
    public virtual string RequestComments { get; set; }

    /// <summary>
    /// LEGACY -- READ ONLY. The entire approval conversation for this appraisal, concatenated into one
    /// 1000-character string. Retained for historical data only; nothing in HCMv3 writes it.
    ///
    /// <para>
    /// This is the pattern <see cref="WorkflowComment"/> replaced, and the reasons are structural rather
    /// than stylistic: one cap for the whole thread means a busy appraisal silently stopped accepting
    /// comments; a concatenated name instead of an employee id means no avatar, no job title and no way to
    /// tell two people with the same name apart; and there is no way to reply to anything, mark a remark
    /// internal to the approvers, or show that it was edited. Everything here is also already recorded
    /// properly in <c>RequestLog</c> for anything raised through the engine.
    /// </para>
    /// </summary>
    [StringLength(1000)]
    public virtual string WorkflowComments { get; set; }

    [StringLength(256)]
    public virtual string ProposedIncrement { get; set; }

    [StringLength(256)]
    public virtual string ProposedJobTitle { get; set; }

    public virtual string AreaOfImprovement { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
