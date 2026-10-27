using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.BenefitsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing company-side insurance installments.
/// </summary>
public class GetAllCompanyInsuranceDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeInsurance.
    /// </summary>
    public int? EmployeeInsuranceId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
