using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing roster group details.
/// </summary>
public class GetAllRosterGroupDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent RosterGroup.
    /// </summary>
    public int? RosterGroupId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
