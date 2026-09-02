using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel header (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.Travel</c>).
///
/// DELIBERATE SIMPLIFICATION (Applicant drop): the legacy entity had an either-or
/// <c>Employee</c>/<c>Applicant</c> relationship (a Travel record could belong to either an existing
/// Employee or a job Applicant from the never-ported Recruitment module). This port drops the
/// <c>Applicant</c> FK entirely and makes <see cref="EmployeeId"/> the only party field (required, not
/// nullable) -- meaning this port only supports travel records for existing employees, not job applicants.
/// This is an acceptable scope reduction given Recruitment is out of scope for this codebase.
///
/// DELIBERATE SIMPLIFICATION (TotalTravelCost rollup): legacy <c>UpdateTotalAmount()</c> eagerly recomputed
/// this field on every save by summing child <see cref="TravelDetail.TotalAmount"/> rows. Following the
/// established precedent in this codebase for identical header/detail amount rollups (see
/// <c>PersonnelAdministration.EmployeeBasicPay.Amount</c> and its detail app service), this field is kept as
/// a stored, but NOT client-editable (absent from <c>CreateOrEditTravelDto</c>), column -- the legacy
/// cross-entity cascade recompute is deliberately NOT re-implemented here.
///
/// <para>
/// <b>THIS ROW IS THE TRAVEL REQUEST.</b> Legacy had a separate <c>TravelDeskRequest</c> header with its own
/// <c>TravelDeskDetailRequest</c> / <c>TravelDeskDetailCostRequest</c> children, and copied the whole triple
/// onto Travel/TravelDetail/TravelDetailCost on approval. That triple is deliberately NOT ported: the trip
/// is created here up front, hidden by the <c>ApprovedOnly</c> filter until the approval chain completes,
/// and approving it writes nothing anywhere else -- the row becoming visible IS the trip being approved.
/// See <c>TravelWorkflowTransactionHandler</c>.
/// </para>
/// <para>
/// The legacy copy step also had a fully swallowing <c>catch (Exception ex) { ex.Message.ToString(); }</c>
/// wrapped round it, so a failure to create the Travel left the request marked approved with nothing
/// written and nothing reported. There is no copy step here to get wrong, and a handler that throws rolls
/// the approval back.
/// </para>
/// </summary>
public class Travel : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TravelTypeId { get; set; }

    public virtual TravelType TravelType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>Legacy display name "Total Travel Cost". See class remarks -- not client-editable.</summary>
    public virtual double TotalTravelCost { get; set; }

    /// <summary>
    /// Why the trip is being made. Carried on the legacy <c>TravelDeskRequest</c> and never copied onto
    /// Travel, so once a trip was approved the reason for it was gone -- the approver read it, the travel
    /// desk never saw it again. It lands on the transaction here for the same reason
    /// <c>EmployeeDocumentMvmt.WithdrawalReason</c> did: when the request and the transaction are one row,
    /// a field that only ever existed on the request has nowhere else to live, and it is worth keeping.
    /// </summary>
    [StringLength(4000)]
    public virtual string TravelPurpose { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>
    /// The legs. A navigation collection so a whole trip can be inserted as one graph -- a travel request
    /// with no legs is not something an approver can decide on, so the children have to exist by the time
    /// the handler validates the submission.
    /// </summary>
    public virtual ICollection<TravelDetail> TravelDetails { get; set; }

    /// <summary>The approval request that governs this row, when it was raised rather than keyed in.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null when entered directly by the travel desk rather than requested. Read by the
    /// ApprovedOnly filter, which is what keeps a pending trip out of every ordinary query -- and, reached
    /// through this header, keeps its legs and cost lines out with it.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
