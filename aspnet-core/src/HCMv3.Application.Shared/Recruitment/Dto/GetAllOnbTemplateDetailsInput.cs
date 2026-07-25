using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing onboarding template details.
/// </summary>
public class GetAllOnbTemplateDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent OnbTemplate.
    /// </summary>
    public int? OnbTemplateId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
