using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee sub group fund pay-band steps.
/// </summary>
public class GetAllEmployeeSubGroupFundsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeSubGroup.
    /// </summary>
    public int? EmployeeSubGroupId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "FromPay ASC";
        }
    }
}
