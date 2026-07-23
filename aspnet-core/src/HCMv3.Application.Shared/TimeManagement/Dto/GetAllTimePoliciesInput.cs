using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing time policies.
/// </summary>
public class GetAllTimePoliciesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / NameEn / NameAr.
    /// </summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
