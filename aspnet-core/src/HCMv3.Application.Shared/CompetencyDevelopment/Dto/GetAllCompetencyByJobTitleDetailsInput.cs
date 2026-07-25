using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing competency-by-job-title detail rows.</summary>
public class GetAllCompetencyByJobTitleDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by parent CompetencyByJobTitle.</summary>
    public int? CompetencyByJobTitleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
