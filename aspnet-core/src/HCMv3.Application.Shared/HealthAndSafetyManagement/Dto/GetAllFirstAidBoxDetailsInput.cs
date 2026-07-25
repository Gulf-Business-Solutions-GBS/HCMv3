using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing first aid box details.
/// </summary>
public class GetAllFirstAidBoxDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? FirstAidBoxId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExpiryDate ASC";
        }
    }
}
