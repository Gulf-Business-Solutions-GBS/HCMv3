using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllRequestLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WorkflowRequestId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ActionDateTime";
        }
    }
}
