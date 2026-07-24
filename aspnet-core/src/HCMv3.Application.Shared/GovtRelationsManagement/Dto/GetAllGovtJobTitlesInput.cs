using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing government job titles.
/// </summary>
public class GetAllGovtJobTitlesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by parent GovtJobCategory.</summary>
    public int? GovtJobCategoryId { get; set; }

    /// <summary>Free-text filter matched against Code / NameEn / NameAr.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
