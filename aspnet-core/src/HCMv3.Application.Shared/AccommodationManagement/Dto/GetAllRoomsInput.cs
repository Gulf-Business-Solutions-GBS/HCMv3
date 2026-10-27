using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing rooms.
/// </summary>
public class GetAllRoomsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public int? FlatId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn ASC";
        }
    }
}
