using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing payroll areas.
/// </summary>
public class GetAllPayrollAreasInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / Name.
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
