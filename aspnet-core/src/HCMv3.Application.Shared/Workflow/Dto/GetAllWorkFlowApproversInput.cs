using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllWorkFlowApproversInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WorkflowRequestId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
