using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing onboarding group details.
/// </summary>
public class GetAllOnbGroupDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent OnbGroup.
    /// </summary>
    public int? OnbGroupId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
