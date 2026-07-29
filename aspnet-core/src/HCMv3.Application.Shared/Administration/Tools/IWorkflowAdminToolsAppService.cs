using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Administration.Tools.Dto;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Ported from legacy <c>HCM10.Web/UserControls/Administration/uscWorkFlowConfigPanel.ascx.cs</c>.
///
/// The control's only server-side function is the "Transfer Leaves" utility; the rest of the file is combo
/// binding and a hardcoded <c>userAdmin</c>/<c>userSuperAdmin</c> visibility check (legacy line 76), which
/// this port replaces with <c>AppPermissions.Pages_Administration_Tools_TransferApprovals</c>.
/// </summary>
public interface IWorkflowAdminToolsAppService : IApplicationService
{
    /// <summary>
    /// Legacy line 96. Reassigns every open, not-yet-approved <c>WorkFlowApprover</c> row from one manager
    /// to another and writes a <c>RequestLog</c> entry of type <c>Transfered</c> for each.
    /// </summary>
    Task<AdminToolResultDto> TransferPendingApprovals(TransferApprovalsInput input);

    /// <summary>
    /// Ported from legacy <c>HCM10.Web/UserControls/uscCopyWorkflowSetup.ascx.cs</c>
    /// (<c>ASPxCallbackPanel1_Callback</c>, lines 89-187): clones a <c>RequestMaster</c> and every one of
    /// its <c>RequestDetail</c> step rows under a new Code/Name, so a workflow can be configured once and
    /// reused for another request type.
    /// </summary>
    Task<AdminToolResultDto> CopyWorkflowSetup(CopyWorkflowSetupInput input);
}
