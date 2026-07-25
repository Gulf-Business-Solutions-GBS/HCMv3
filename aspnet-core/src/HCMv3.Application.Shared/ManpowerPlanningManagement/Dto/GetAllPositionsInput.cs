using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing positions.
/// </summary>
public class GetAllPositionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public bool? Vacant { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn";
        }
    }
}
