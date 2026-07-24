using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing sponsor job titles.
/// </summary>
public class GetAllSponsorJobTitlesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by parent Sponsor.</summary>
    public int? SponsorId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
