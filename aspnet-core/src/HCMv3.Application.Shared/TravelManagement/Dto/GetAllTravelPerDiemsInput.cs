using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing travel per-diem rates.
/// </summary>
public class GetAllTravelPerDiemsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? FromDestinationId { get; set; }

    public int? ToDestinationId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
