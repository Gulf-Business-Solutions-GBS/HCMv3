using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllRequestDetailParallelsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? RequestDetailId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
