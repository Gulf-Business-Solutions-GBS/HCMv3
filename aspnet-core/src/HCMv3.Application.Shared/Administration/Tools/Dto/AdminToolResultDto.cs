using System.Collections.Generic;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Common result shape for every ported legacy Administration utility.
///
/// The legacy panels reported outcomes by writing into an <c>ASPxLabel</c> (<c>lblStatus</c>,
/// <c>lbl_msg_SalRegDel</c>, <c>Labelcontent2_status</c>, ...) and colouring it green/red. That is a
/// WebForms presentation concern; this port returns a structured result and lets Angular render it.
/// </summary>
public class AdminToolResultDto
{
    /// <summary>Number of rows created/updated/deleted, depending on the operation.</summary>
    public int AffectedCount { get; set; }

    /// <summary>Number of rows skipped because they already satisfied the target state.</summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Per-item detail lines (employee code + what happened). Legacy discarded these; keeping them makes
    /// these destructive tools auditable from the UI.
    /// </summary>
    public List<string> Messages { get; set; } = new List<string>();
}
