using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing flats.
/// </summary>
public class GetAllFlatsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public int? FloorId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn ASC";
        }
    }
}
