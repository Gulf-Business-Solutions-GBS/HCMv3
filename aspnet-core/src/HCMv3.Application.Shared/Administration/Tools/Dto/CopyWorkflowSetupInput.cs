using System.ComponentModel.DataAnnotations;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Input for the "clone a workflow request setup" utility, ported from legacy
/// <c>HCM10.Web/UserControls/uscCopyWorkflowSetup.ascx.cs</c> (the <c>ddsrcRequest</c> /
/// <c>txtRequestCode</c> / <c>txtRequestName</c> form).
/// </summary>
public class CopyWorkflowSetupInput
{
    /// <summary>Legacy <c>ddsrcRequest</c> -- the <c>RequestMaster</c> to clone FROM.</summary>
    [Required]
    public int SourceRequestMasterId { get; set; }

    /// <summary>Legacy <c>txtRequestCode</c>. Legacy trims it (line 124) and rejects duplicates.</summary>
    [Required]
    [StringLength(64)]
    public string NewRequestCode { get; set; }

    /// <summary>Legacy <c>txtRequestName</c>, written to <c>RequestMaster.NameEn</c>.</summary>
    [Required]
    [StringLength(256)]
    public string NewRequestNameEn { get; set; }

    /// <summary>
    /// This port's addition. <c>RequestMaster.NameAr</c> is <c>[Required]</c> in this codebase but legacy's
    /// form had no Arabic field, so when this is blank the English name is copied into it (a required
    /// column cannot be left null). Legacy never set it at all.
    /// </summary>
    [StringLength(256)]
    public string NewRequestNameAr { get; set; }
}
