using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing onboarding video links.
/// </summary>
public class GetAllOnbVideoLinksInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn";
        }
    }
}
