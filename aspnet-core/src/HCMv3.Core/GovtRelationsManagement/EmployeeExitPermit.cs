using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// An employee's exit permit and the internal sign-off it needs (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.GovtRelations.ExitPermitRequest</c>).
///
/// <para>
/// <b>The permit is not issued here.</b> It comes from the government, outside this system, and the employee
/// already has its number in hand before they open the screen -- which is why
/// <see cref="ExternalRequestNumber"/> is keyed in and required rather than generated. What HCMv3 adds is the
/// internal record and the routing: the company still wants a manager to see and agree the absence, even
/// though nobody here grants or refuses the permit itself.
/// </para>
///
/// <para>
/// <b>Approval writes nothing anywhere else,</b> and that is deliberate. The legacy approval block on this
/// object was commented-out copy-paste borrowed from a different transaction type; reviving it would invent
/// a side effect the business never had. The record simply becomes visible once granted.
/// </para>
///
/// <para>
/// One row, not two. Legacy paired an <c>ExitPermitRequest</c> with a separate transaction; here the request
/// and the record are the same row and <see cref="WorkflowStatus"/> is what hides it until approved --
/// see <see cref="IWorkflowEnabledEntity"/>.
/// </para>
/// </summary>
public class EmployeeExitPermit : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// The permit number the government issued. Typed in by the requester because it exists before the
    /// record does; required, since a permit record with no permit number is nothing an approver can check.
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string ExternalRequestNumber { get; set; }

    public virtual TravelReason TravelReason { get; set; }

    /// <summary>The date the employee leaves the country, as printed on the permit.</summary>
    public virtual DateTime ExitDate { get; set; }

    /// <summary>When the permit takes effect for the company's own records.</summary>
    public virtual DateTime StartDate { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this row, when it was raised through self service.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null when entered directly by HR rather than requested. Read by the ApprovedOnly filter,
    /// which is what keeps a pending row out of the rest of the system.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
