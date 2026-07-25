using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing job descriptions.
/// </summary>
public class GetAllJobDescriptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ShortDescriptionEn";
        }
    }
}
