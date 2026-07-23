using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence violation rule exceptions.
/// </summary>
public class GetAllAbsenceViolationRuleExceptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent AbsenceViolationRule.
    /// </summary>
    public int? AbsenceViolationRuleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
