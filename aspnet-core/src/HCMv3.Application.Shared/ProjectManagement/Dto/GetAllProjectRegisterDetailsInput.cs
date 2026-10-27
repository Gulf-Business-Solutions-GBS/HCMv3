using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing project register details.
/// </summary>
public class GetAllProjectRegisterDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent ProjectRegister.
    /// </summary>
    public int? ProjectRegisterId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
