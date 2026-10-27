using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing project logs.
/// </summary>
public class GetAllProjectLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RunTimeStamp DESC";
        }
    }
}
