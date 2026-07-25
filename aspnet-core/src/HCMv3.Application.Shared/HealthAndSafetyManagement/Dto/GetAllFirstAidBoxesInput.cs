using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing first aid boxes.
/// </summary>
public class GetAllFirstAidBoxesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public int? LocationId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
