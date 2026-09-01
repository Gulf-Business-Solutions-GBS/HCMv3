using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// An employee's exit clearance: the checklist IT, Finance, HR and whoever else sign off before somebody
/// leaves (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.ClearanceRequest</c>).
///
/// <para>
/// <b>Nothing here knows about departments.</b> Legacy had no bespoke multi-department sign-off and neither
/// does this. What makes several departments clear the same employee at once is an ordinary
/// <c>ApprovalRequire.Parallel</c> step on the type's <c>RequestMaster</c>: the engine assigns every role
/// holder as an approver of the same step and holds the step open until all of them have acted. Which
/// checklist lines each of them is shown comes from <see cref="ClearanceRequestFieldDetail.WorkflowRoleId"/>,
/// not from anything on this row.
/// </para>
///
/// <para>
/// One row, not two: the request and the record are the same row and <see cref="WorkflowStatus"/> is what
/// keeps it out of the rest of the system until it is approved -- see <see cref="IWorkflowEnabledEntity"/>.
/// The answers themselves hang off <see cref="EmployeeClearanceDetail"/>.
/// </para>
/// </summary>
public class EmployeeClearance : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>The employee being cleared -- the person leaving, not whoever is signing.</summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// Which checklist to use. Also the workflow sub-type, so a resignation clearance can route through a
    /// different chain from an end-of-contract one.
    /// </summary>
    public virtual int ClearanceRequestTypeId { get; set; }

    public virtual ClearanceRequestType ClearanceRequestType { get; set; }

    /// <summary>When the clearance period opens.</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>Last working day the clearance covers. Never before <see cref="StartDate"/>.</summary>
    public virtual DateTime EndDate { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this row, when it was raised through self service.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null when entered directly by HR rather than requested. Read by the ApprovedOnly filter,
    /// which is what keeps a pending clearance out of the rest of the system.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
