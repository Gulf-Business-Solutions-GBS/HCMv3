using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllRequestDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? RequestMasterId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Step";
        }
    }
}
