using System.ComponentModel.DataAnnotations;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Input for the legacy uscWorkFlowConfigPanel "Transfer Leaves" panel
/// (<c>ASPxCallbackPanel1_Callback</c>, line 96) — despite the caption it transfers ALL open workflow
/// approvals, not just leave requests.
///
/// DEVIATION: legacy bound Employee <c>Code</c> strings into combos and re-queried by code. Ported as Ids.
/// </summary>
public class TransferApprovalsInput
{
    /// <summary>Legacy <c>cmbTransferFromManager</c>: the approver whose open items move away.</summary>
    [Range(1, int.MaxValue)]
    public int FromApproverEmployeeId { get; set; }

    /// <summary>Legacy <c>cmbTransferToManager</c>: the approver who receives them.</summary>
    [Range(1, int.MaxValue)]
    public int ToApproverEmployeeId { get; set; }

    /// <summary>
    /// Legacy <c>cmbEmployees</c> (optional, "N/A" = all). When set, only that requester's open approvals
    /// are transferred.
    /// </summary>
    public int? RequestingEmployeeId { get; set; }
}
