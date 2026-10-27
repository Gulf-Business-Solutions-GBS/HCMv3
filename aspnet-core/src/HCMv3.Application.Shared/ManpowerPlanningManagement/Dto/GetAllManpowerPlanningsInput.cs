using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing manpower planning requests.
/// </summary>
public class GetAllManpowerPlanningsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public long? OrganizationUnitId { get; set; }

    public int? PeriodId { get; set; }

    public ManpowerPlanningStatus? Status { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
