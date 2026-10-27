using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Manpower planning request header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanning</c>, which inherited from
/// <c>Workflow.WorkflowRequest</c>).
///
/// <para>
/// ROUTED THROUGH THE WORKFLOW ENGINE. The first port of this entity carried its own stored
/// <see cref="ManpowerPlanningStatus"/> column AND its own Approve/Reject app-service methods, because the
/// workflow module did not exist yet. Both are gone. <see cref="WorkflowStatus"/> is now the only stored
/// approval state and <see cref="Status"/> is a read-only projection of it: a record with two statuses and
/// two approval paths is a record whose two statuses eventually disagree, and the disagreement is silent.
/// </para>
///
/// <para>
/// A manpower plan is raised by a manager or by HR about a department, never by an employee about
/// themselves, so it is workflow-capable but deliberately NOT self-service -- its catalog row carries no ESS
/// route. See <c>ManpowerPlanningWorkflowTransactionHandler</c>.
/// </para>
///
/// <para>
/// <c>OrganizationUnitId</c> FKs Zero's own built-in <c>Abp.Organizations.OrganizationUnit</c> tree (the
/// legacy "Department" field), matching the same simplification already used by
/// <c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c>.
/// </para>
/// </summary>
public class ManpowerPlanning : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    [Required]
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [Required]
    public virtual long OrganizationUnitId { get; set; }

    public virtual Abp.Organizations.OrganizationUnit OrganizationUnit { get; set; }

    [Required]
    public virtual int PeriodId { get; set; }

    public virtual ManpowerPlanningPeriod Period { get; set; }

    public virtual double GrandTotalAmount { get; set; }

    [StringLength(1000)]
    public virtual string RequestComments { get; set; }

    /// <summary>The approval request that governs this plan, when the type is workflow-enabled.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>
    /// The single stored approval state, written only by the workflow engine (through
    /// <c>WorkflowTransactionHandlerBase</c>). Null when the type is not routed, in which case the plan is
    /// immediately effective -- the pre-workflow behaviour. Read by the ApprovedOnly filter, which is what
    /// keeps a headcount plan nobody agreed to out of every ordinary query.
    /// </summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// The module's own status vocabulary, DERIVED from <see cref="WorkflowStatus"/> and stored nowhere.
    ///
    /// <para>
    /// Read-only on purpose: it cannot be written, so it cannot disagree with the engine. That is the whole
    /// reason it is a projection rather than a column -- the previous stored version could be set, and was
    /// set, by an approval path the engine knew nothing about.
    /// </para>
    ///
    /// <para>
    /// Not mapped by EF (ignored in <c>HCMv3DbContext</c>), so it cannot appear in a database query.
    /// Queries that need to filter by it filter on <see cref="WorkflowStatus"/> instead -- see
    /// <see cref="ToWorkflowStatuses"/>.
    /// </para>
    /// </summary>
    public ManpowerPlanningStatus Status => FromWorkflowStatus(WorkflowStatus);

    public virtual ICollection<ManpowerPlanningDetail> ManpowerPlanningDetails { get; set; }

    /// <summary>
    /// The one and only place the engine's vocabulary is translated into this module's. Draft or absent
    /// means nobody has been asked yet; Open means somebody has; Closed is an approval that has since been
    /// closed out and is still an approval.
    /// </summary>
    public static ManpowerPlanningStatus FromWorkflowStatus(RequestStatus? workflowStatus)
    {
        switch (workflowStatus)
        {
            case null:
            case RequestStatus.Draft:
                return ManpowerPlanningStatus.Draft;

            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return ManpowerPlanningStatus.Approved;

            case RequestStatus.Rejected:
                return ManpowerPlanningStatus.Rejected;

            default:
                // Open, Transferred -- in flight, sitting in somebody's inbox.
                return ManpowerPlanningStatus.Submitted;
        }
    }

    /// <summary>
    /// The inverse, for querying: which stored <see cref="RequestStatus"/> values a caller asking for the
    /// given <see cref="ManpowerPlanningStatus"/> means. The null entry is meaningful -- a row that was
    /// never routed reads as Draft -- so it is returned and callers must handle it.
    /// </summary>
    public static RequestStatus?[] ToWorkflowStatuses(ManpowerPlanningStatus status)
    {
        switch (status)
        {
            case ManpowerPlanningStatus.Draft:
                return new RequestStatus?[] { null, RequestStatus.Draft };

            case ManpowerPlanningStatus.Submitted:
                return new RequestStatus?[] { RequestStatus.Open, RequestStatus.Transferred };

            case ManpowerPlanningStatus.Approved:
                return new RequestStatus?[] { RequestStatus.Approved, RequestStatus.Closed };

            case ManpowerPlanningStatus.Rejected:
                return new RequestStatus?[] { RequestStatus.Rejected };

            default:
                return new RequestStatus?[0];
        }
    }
}
