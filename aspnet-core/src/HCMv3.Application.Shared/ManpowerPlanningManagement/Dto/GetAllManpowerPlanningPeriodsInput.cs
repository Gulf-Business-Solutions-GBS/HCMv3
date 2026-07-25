using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing manpower planning periods.
/// </summary>
public class GetAllManpowerPlanningPeriodsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Name";
        }
    }
}
