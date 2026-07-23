using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence deduction rules.
/// </summary>
public class GetAllAbsenceDeductionRulesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / RuleName.
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
