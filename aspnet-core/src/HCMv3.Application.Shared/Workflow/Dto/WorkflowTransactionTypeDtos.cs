using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class WorkflowTransactionTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsWorkflowEnabled { get; set; }

    public bool IsEssEnabled { get; set; }

    public int? DefaultRequestMasterId { get; set; }

    public string DefaultRequestMasterNameEn { get; set; }

    /// <summary>Number of approval steps on the default chain -- shown so an admin can see at a glance
    /// whether a type is actually routed anywhere.</summary>
    public int? DefaultRequestMasterSteps { get; set; }

    public string EssRoute { get; set; }

    public string EssPermissionName { get; set; }

    public string SubTypeSource { get; set; }

    public string IconClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Whether somebody with the <c>RecordWithoutApproval</c> permission may record this type for an
    /// employee as final instead of routing it. Off by default; see the entity for why both gates exist.
    /// </summary>
    public bool AllowDirectEntry { get; set; }
}

public class CreateOrEditWorkflowTransactionTypeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// The entity type name, e.g. "EmployeeAbsence". Must match <c>WorkflowRequest.RequestType</c> and
    /// <c>WorkflowGroupDetail.RequestTypeName</c> exactly, or routing will not find this row.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Code { get; set; }

    [Required]
    [StringLength(128)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(128)]
    public string NameAr { get; set; }

    public bool IsWorkflowEnabled { get; set; }

    public bool IsEssEnabled { get; set; }

    public int? DefaultRequestMasterId { get; set; }

    [StringLength(256)]
    public string EssRoute { get; set; }

    [StringLength(256)]
    public string EssPermissionName { get; set; }

    [StringLength(128)]
    public string SubTypeSource { get; set; }

    [StringLength(128)]
    public string IconClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether somebody with the <c>RecordWithoutApproval</c> permission may record this type for an
    /// employee as final instead of routing it. Off by default; see the entity for why both gates exist.
    /// </summary>
    public bool AllowDirectEntry { get; set; }
}

public class GetWorkflowTransactionTypeForEditOutput
{
    public CreateOrEditWorkflowTransactionTypeDto WorkflowTransactionType { get; set; }
}

public class GetAllWorkflowTransactionTypesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public bool? IsWorkflowEnabled { get; set; }

    public bool? IsEssEnabled { get; set; }

    public bool? IsActive { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "DisplayOrder, Code";
        }
    }
}
