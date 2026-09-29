using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Workflow;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// One approver position on a chain, in the engine's own vocabulary.
///
/// <para>
/// <c>ApproverPerson</c> says WHAT KIND of approver: <c>Person</c> means whoever currently holds a
/// workflow role, and <c>PersonManager</c> / <c>ManagerLevel2..7</c> walk the requester's reporting line.
/// <c>WorkflowRoleId</c> is required for <c>Person</c> and meaningless for the rest.
/// </para>
///
/// <para>
/// There is deliberately no "this employee approves" option, because the engine has no such field. Naming
/// one person means a role with one member -- which is also what survives that person leaving.
/// </para>
/// </summary>
public class WorkflowSetupStepDto
{
    public WorkflowApproverPersonType ApproverPerson { get; set; }

    public int? WorkflowRoleId { get; set; }

    public string WorkflowRoleName { get; set; }

    /// <summary>
    /// When the role has several holders: <c>Single</c> means any one of them may decide, <c>All</c> means
    /// every one must. Ignored for manager positions, which resolve to exactly one person.
    /// </summary>
    public ApprovalRequire ApprovalRequire { get; set; }

    /// <summary>Members of the role today, purely so the grid can warn about an empty one.</summary>
    public int CurrentHolderCount { get; set; }
}

/// <summary>
/// One row of the Workflow Setup grid: a transaction, its approvers in order, and the switches that decide
/// where it can be raised.
/// </summary>
public class WorkflowSetupRowDto
{
    /// <summary>The catalog code, which is the entity class name. Fixed -- the grid never creates types.</summary>
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Approvers in order. Empty means the transaction is not routed at all.</summary>
    public List<WorkflowSetupStepDto> Steps { get; set; } = new List<WorkflowSetupStepDto>();

    /// <summary>
    /// Who may raise this, from the chain's creator row. <c>None</c> means the chain does not restrict it.
    /// </summary>
    public WorkflowPersonScope RaisedBy { get; set; }

    /// <summary>
    /// True when this transaction follows the shared default chain rather than one of its own.
    ///
    /// <para>
    /// Its approver cells then show the default's, read-only: the grid exists to answer "who approves
    /// this", and hiding the answer behind another panel would defeat that.
    /// </para>
    /// </summary>
    public bool UsesDefaultChain { get; set; }

    public bool IsWorkflowEnabled { get; set; }

    public bool IsEssEnabled { get; set; }

    /// <summary>HR may record this as final instead of sending it for approval.</summary>
    public bool AllowDirectEntry { get; set; }

    public bool NotifyByEmail { get; set; }

    /// <summary>Days before an unanswered request escalates. Zero means never.</summary>
    public int EscalateAfterDays { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// True when this row cannot be edited from the grid without destroying configuration the grid cannot
    /// express. <see cref="LockedReason"/> says which. The detailed screens remain the way in.
    /// </summary>
    public bool IsLocked { get; set; }

    public string LockedReason { get; set; }

    /// <summary>
    /// Things worth knowing that do not stop an edit: a role with nobody in it, employees with no manager,
    /// or routing that something else may override at run time.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
}

public class GetWorkflowSetupGridOutput
{
    public List<WorkflowSetupRowDto> Rows { get; set; } = new List<WorkflowSetupRowDto>();

    /// <summary>
    /// The shared default chain every transaction can be pointed at, so most of them need no chain of
    /// their own. Empty until somebody defines it.
    /// </summary>
    public List<WorkflowSetupStepDto> DefaultChainSteps { get; set; } = new List<WorkflowSetupStepDto>();

    /// <summary>How many transactions follow the default today, so editing it can say what it will change.</summary>
    public int DefaultChainUsedByCount { get; set; }

    /// <summary>Every role that can be chosen, so the grid does not fetch them per cell.</summary>
    public List<WorkflowSetupRoleDto> Roles { get; set; } = new List<WorkflowSetupRoleDto>();

    /// <summary>
    /// True when workflow groups are switched on for this tenant, which means a group chain can outrank
    /// what the grid sets. The screen says so once rather than on every affected row.
    /// </summary>
    public bool WorkflowGroupsEnabled { get; set; }
}

public class WorkflowSetupRoleDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>Members whose assignment covers today.</summary>
    public int CurrentHolderCount { get; set; }
}

/// <summary>Saves one row. The grid saves a row at a time, because one row is one decision.</summary>
public class SaveWorkflowSetupRowInput
{
    [Required]
    [StringLength(128)]
    public string Code { get; set; }

    /// <summary>
    /// Point this transaction at the shared default chain instead of giving it one of its own. When true
    /// <see cref="Steps"/> is ignored -- the default decides.
    /// </summary>
    public bool UseDefaultChain { get; set; }

    public List<WorkflowSetupStepDto> Steps { get; set; } = new List<WorkflowSetupStepDto>();

    public WorkflowPersonScope RaisedBy { get; set; }

    public bool IsWorkflowEnabled { get; set; }

    public bool IsEssEnabled { get; set; }

    public bool AllowDirectEntry { get; set; }

    public bool NotifyByEmail { get; set; }

    public int EscalateAfterDays { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Creates a workflow role and puts people in it, so a chain can be finished without leaving the grid.
/// </summary>
public class CreateWorkflowSetupRoleInput
{
    [Required]
    [StringLength(6)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    /// <summary>
    /// Scopes the role to a company, department, branch or cost centre, exactly as the Workflow Roles
    /// screen does. None means it applies everywhere.
    /// </summary>
    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    /// <summary>The organization unit this role belongs to, when it is scoped to one.</summary>
    public int? WorkflowRoleOrgId { get; set; }

    /// <summary>
    /// Who holds it. May be empty -- a role with nobody in it is a legitimate half-finished state, and the
    /// grid warns about it rather than refusing to save.
    /// </summary>
    public List<int> EmployeeIds { get; set; } = new List<int>();
}

/// <summary>One holder of a workflow role, for the members panel.</summary>
public class WorkflowSetupRoleMemberDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }
}

/// <summary>
/// Defines the shared default chain -- the one most transactions follow, so that setting approvals up is
/// one decision rather than thirty.
/// </summary>
public class SaveWorkflowSetupDefaultChainInput
{
    /// <summary>
    /// Its approver positions, in order. Saving it with none is how the default is cleared, and any
    /// transaction still pointing at it is moved off it rather than left routing to nobody.
    /// </summary>
    public List<WorkflowSetupStepDto> Steps { get; set; } = new List<WorkflowSetupStepDto>();

    public bool NotifyByEmail { get; set; }

    public int EscalateAfterDays { get; set; }
}
