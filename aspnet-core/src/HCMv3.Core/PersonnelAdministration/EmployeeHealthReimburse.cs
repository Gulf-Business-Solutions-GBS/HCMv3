using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee health reimbursement claim record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeHealthReimburse</c>, Wave 4 deferred
/// bucket).
///
/// <para>
/// <b>THIS ROW IS THE CLAIM.</b> Legacy had a second table, <c>HealthReimburseRequest</c>, which an employee
/// filed and which -- on approval -- COPIED its fields onto a freshly created <c>EmployeeHealthReimburse</c>.
/// That shadow-table pair is exactly what this architecture removes: there is no
/// <c>HealthReimburseRequest</c> here, and nothing is created on approval. The claim is inserted into this
/// table straight away with <see cref="WorkflowStatus"/> = Draft/Open, which the <c>ApprovedOnly</c> filter
/// hides from payroll, reports and the wage-type calculator until an approver agrees to it. The single
/// biggest benefit is that the balance calculation can see pending claims, because they are sitting right
/// here next to the approved ones -- in the two-table design they were somewhere the quota arithmetic could
/// not reach.
/// </para>
///
/// <para>
/// <b>The quota enforcement IS ported</b> (it was previously recorded here as a deliberate simplification,
/// on the grounds that <c>EmployeeSubGroup</c>, <c>EmployeeDependant</c>, <c>EmployeeDependantException</c>
/// and <c>EmployeeBenefitException</c> had not been ported. All four have since landed, so the gap closed).
/// It lives in <c>IHealthReimburseQuotaService</c> rather than on this entity, in keeping with every other
/// entity here: the arithmetic belongs to a service the workflow handler, the ESS screen and the admin
/// screen can all call, not to an <c>OnSaving</c> override that only one write path would go through.
/// </para>
///
/// <para>
/// STILL NOT PORTED: the legacy <c>DocAttachment</c> FK.
/// </para>
/// </summary>
public class EmployeeHealthReimburse : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this claim, when it was raised through self service or
    /// routed by HR.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>
    /// Null when the claim was entered directly by HR with the transaction type dormant -- i.e. effective at
    /// once, the pre-workflow behaviour. Read by the <c>ApprovedOnly</c> filter, which is what keeps a
    /// pending claim out of payroll AND out of the "used" side of the balance until it is approved.
    /// </summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
