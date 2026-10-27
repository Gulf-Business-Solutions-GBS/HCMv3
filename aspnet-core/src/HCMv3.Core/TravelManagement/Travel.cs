using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Recruitment;
using HCMv3.Workflow;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel header (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.Travel</c>).
///
/// EMPLOYEE OR APPLICANT. Legacy's Travel could belong to an existing <see cref="Employee"/> or to a job
/// <see cref="Applicant"/> (a candidate flown in for interview, a new hire travelling before joining). Both
/// party columns are nullable here and <c>TravelsAppService</c> requires exactly one of them
/// (<see cref="TravelParty.HasExactlyOne"/>). Legacy itself only refused "both blank"
/// (<c>Travel.PerformValidation</c>: "Employee and Applicant both together should not be blank."); the
/// stricter rule is deliberate, because a row naming both is ambiguous about whose trip it is, whose per
/// diem applies and whose claim matches it.
///
/// An applicant trip has no employee, so everything legacy keyed on <c>Travel.Employee</c> degrades the way
/// legacy's did: per diem is zero (legacy <c>TravelDetail.PerDiemRate</c>/<c>PerDiemAmount</c> return 0 when
/// <c>Travel.Employee == null</c>), cost totals are left unrounded (legacy <c>CurrencyService.Rounding</c>
/// returns the value as-is for a null employee), and the trip never appears in self service, in the leave
/// clash check or under an employee in the travel report.
///
/// <para><b><see cref="IHasEmployeeId"/> is implemented explicitly</b> and reads 0 for an applicant trip, so
/// the in-memory "is this row mine?" checks in the shared self-service and attachment code never match one.
/// Do NOT pass this type to <c>EmployeeScopeFilter.Apply</c> or <c>MyRowsQuery</c>: those bind to the column
/// by name and the column is <c>int?</c>. Query <see cref="EmployeeId"/> directly instead.</para>
///
/// TotalTravelCost rollup: stored, NOT client-editable (absent from <c>CreateOrEditTravelDto</c>), and kept
/// in line with the legs by <see cref="ITravelCostRollupService"/> whenever the trip, a leg or a cost line
/// is saved or deleted -- the port of legacy's three <c>UpdateTotalAmount()</c> methods.
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

    /// <summary>The employee travelling. Null for an applicant trip -- see class remarks.</summary>
    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>The job applicant travelling, when the trip is not an employee's (legacy <c>Travel.Applicant</c>).</summary>
    public virtual int? ApplicantId { get; set; }

    public virtual Applicant Applicant { get; set; }

    /// <summary>0 for an applicant trip, so no employee ever matches it. See class remarks.</summary>
    int IHasEmployeeId.EmployeeId => EmployeeId ?? 0;

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
