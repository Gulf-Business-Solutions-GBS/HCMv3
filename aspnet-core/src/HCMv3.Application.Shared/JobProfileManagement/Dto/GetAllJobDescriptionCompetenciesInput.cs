using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing a <see cref="JobDescription"/>'s competency rows.
/// </summary>
public class GetAllJobDescriptionCompetenciesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? JobDescriptionId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
