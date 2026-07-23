using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing salary registers.
/// </summary>
public class GetAllSalaryRegistersInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "InPeriod DESC";
        }
    }
}
