using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing travel details.
/// </summary>
public class GetAllTravelDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? TravelId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "DepartureDate ASC";
        }
    }
}
