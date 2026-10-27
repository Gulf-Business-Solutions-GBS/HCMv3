using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound delegation of an Employee's duties to another Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDelegation</c>).
///
/// <para>
/// <b>There is no DelegationRequest table.</b> Legacy had one, and it did nothing but copy its own fields
/// onto an <c>EmployeeDelegation</c> when it reached Approved. This row IS the request -- the two columns
/// below are the entire cost of routing it, and approval has nothing to copy.
/// </para>
///
/// <para>
/// <b>What approval actually buys here is unusual and worth stating.</b> Most workflow-enabled rows are
/// hidden until approved so that payroll or a quota does not act on them early. This one is hidden so that
/// the <em>approval system itself</em> does not act on it early: an active delegation row is what
/// <c>EmailService</c> reads to copy a covering colleague on somebody's notifications, and it is the row a
/// future approver-substitution branch would read. A pending delegation that were visible would hand a
/// colleague authority the moment it was typed -- before anybody agreed to the cover. The
/// <c>ApprovedOnly</c> filter on <see cref="WorkflowStatus"/> is what makes "asking for cover" and "having
/// cover" two different states, which they were not before.
/// </para>
/// </summary>
public class EmployeeDelegation : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DelegateToId { get; set; }

    public virtual Employee DelegateTo { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
