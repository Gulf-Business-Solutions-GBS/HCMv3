using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing indemnity pay rule details.
/// </summary>
public class GetAllIndemnityPayRuleDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? IndemnityPayRuleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "MinYearOfService ASC";
        }
    }
}
