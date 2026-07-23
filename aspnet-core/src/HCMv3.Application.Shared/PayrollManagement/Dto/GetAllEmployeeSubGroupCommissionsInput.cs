using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee sub group commissions.
/// </summary>
public class GetAllEmployeeSubGroupCommissionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeSubGroup.
    /// </summary>
    public int? EmployeeSubGroupId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
