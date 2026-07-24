using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllPATimelinesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PAPeriodId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
