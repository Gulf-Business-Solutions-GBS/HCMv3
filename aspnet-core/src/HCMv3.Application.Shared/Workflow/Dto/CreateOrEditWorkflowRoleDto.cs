using System.ComponentModel.DataAnnotations;
using HCMv3.Workflow;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkflowRoleDto
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
    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public int? WorkflowRoleOrgId { get; set; }
}
