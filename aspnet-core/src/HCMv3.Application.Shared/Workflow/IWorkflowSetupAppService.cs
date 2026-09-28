using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

/// <summary>
/// The Workflow Setup grid: one row per transaction, approvers across, switches at the end.
///
/// <para>
/// A wrapper over the detailed screens, not a replacement for them. It writes the same four tables --
/// <c>WorkflowTransactionType</c>, <c>RequestMaster</c>, <c>RequestDetail</c> and <c>WorkflowRole</c> --
/// and adds no storage of its own. Anything it saves can be opened and refined in the detailed screens;
/// anything the detailed screens can express that the grid cannot is locked here rather than flattened.
/// </para>
///
/// <para>
/// The point is that switching a transaction on should not require understanding chains, steps, approver
/// person types and role assignment as four separate ideas across four screens.
/// </para>
/// </summary>
public interface IWorkflowSetupAppService : IApplicationService
{
    /// <summary>
    /// Every catalogued transaction as a grid row, with the roles to choose from and whatever warnings
    /// apply. Rows the grid cannot safely edit come back locked, with the reason.
    /// </summary>
    Task<GetWorkflowSetupGridOutput> GetGrid();

    /// <summary>
    /// Saves one row: creates or updates that transaction's own chain, replaces its steps, and sets the
    /// catalog switches.
    ///
    /// <para>
    /// Refuses a locked row rather than overwriting it, and refuses a chain whose steps would not resolve
    /// to anybody -- a configuration that looks saved and then fails at the first request is worse than
    /// one that would not save.
    /// </para>
    /// </summary>
    Task SaveRow(SaveWorkflowSetupRowInput input);

    /// <summary>Creates a role and its members without leaving the grid.</summary>
    Task<WorkflowSetupRoleDto> CreateRole(CreateWorkflowSetupRoleInput input);

    /// <summary>Who currently holds a role, for the panel that edits its members.</summary>
    Task<List<WorkflowSetupRoleMemberDto>> GetRoleMembers(int workflowRoleId);
}
