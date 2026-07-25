using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing application statuses.
/// </summary>
public class GetAllApplicationStatusesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent Vacancy.
    /// </summary>
    public int? VacancyId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
