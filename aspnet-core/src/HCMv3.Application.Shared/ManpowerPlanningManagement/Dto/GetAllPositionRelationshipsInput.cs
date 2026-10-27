using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing position relationships (holders).
/// </summary>
public class GetAllPositionRelationshipsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PositionId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
