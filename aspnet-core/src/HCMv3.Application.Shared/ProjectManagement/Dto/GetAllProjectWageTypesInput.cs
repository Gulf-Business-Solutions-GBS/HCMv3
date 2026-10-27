using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing project wage types.
/// </summary>
public class GetAllProjectWageTypesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Project.
    /// </summary>
    public int? ProjectId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
