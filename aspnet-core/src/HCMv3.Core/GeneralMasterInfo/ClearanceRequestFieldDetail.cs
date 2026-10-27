using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Says WHICH DEPARTMENT owns one line of a clearance checklist (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.ClearanceRequestFieldDetail</c>).
///
/// <para>
/// A <see cref="ClearanceRequestField"/> is a single question on the exit checklist -- "laptop returned",
/// "final settlement calculated", "access badge handed in". This row attaches that question to a
/// <see cref="Workflow.WorkflowRole"/>, and that attachment is the whole point of the table: it is what
/// makes IT answer the IT lines and Finance answer the Finance lines. When a clearance sits on a
/// <c>ApprovalRequire.Parallel</c> step every department's approver is looking at the same record at the
/// same time, and each is shown only the lines whose role they hold.
/// </para>
///
/// <para>
/// One field may have several of these rows, which is how a line that two departments must both confirm is
/// expressed. No bespoke multi-department sign-off mechanism is involved anywhere: the generic engine's
/// parallel step does the routing, and this FK does the filtering.
/// </para>
/// </summary>
public class ClearanceRequestFieldDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? ClearanceRequestFieldId { get; set; }

    public virtual ClearanceRequestField ClearanceRequestField { get; set; }

    /// <summary>
    /// The department that owns this checklist line, expressed as the approver role that department's staff
    /// hold. Required: a detail row without it tells nobody anything, which is exactly the state this table
    /// was left in while the workflow module was out of scope.
    /// </summary>
    public virtual int WorkflowRoleId { get; set; }

    public virtual WorkflowRole WorkflowRole { get; set; }
}
