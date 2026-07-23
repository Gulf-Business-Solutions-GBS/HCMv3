using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee sub group benefit details.
/// </summary>
public class GetAllEmployeeSubGroupBenefitDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeSubGroupBenefit.
    /// </summary>
    public int? EmployeeSubGroupBenefitId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
