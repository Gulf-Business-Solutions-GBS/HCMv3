using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing competency-by-job-title profiles.</summary>
public class GetAllCompetencyByJobTitlesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by JobTitle.</summary>
    public int? JobTitleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
