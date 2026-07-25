using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditRequestMasterDto
{
    public int? Id { get; set; }

    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [Required]
    public WorkflowSecurityLevel SecurityLevel { get; set; }

    public bool AutoClose { get; set; }

    public int AutoCloseInDays { get; set; }

    public int? DefaultApproverId { get; set; }

    public bool IsCommentRequire { get; set; }

    public int DueInDays { get; set; }
}
