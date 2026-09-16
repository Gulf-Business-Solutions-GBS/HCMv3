using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence deduction rule details.
/// </summary>
public class GetAllAbsenceDedRuleDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent AbsenceDeductionRule.
    /// </summary>
    public int? AbsenceDeductionRuleId { get; set; }

    /// <summary>
    /// A detail line has no names, so the search is by value: a number matches the days count or the deduction
    /// number, a date (yyyy-MM-dd) matches the lines whose period includes it, and a year the lines touching it.
    /// </summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
