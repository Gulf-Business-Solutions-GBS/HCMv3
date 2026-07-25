using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllWorkflowGroupDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WorkflowGroupId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RequestTypeName";
        }
    }
}
