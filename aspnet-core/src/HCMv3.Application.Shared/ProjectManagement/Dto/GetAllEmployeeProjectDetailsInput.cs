using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee project details.
/// </summary>
public class GetAllEmployeeProjectDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeProject.
    /// </summary>
    public int? EmployeeProjectId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate";
        }
    }
}
