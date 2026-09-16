using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing wage type exceptions.
/// </summary>
public class GetAllWageTypeExceptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WageTypeId { get; set; }

    /// <summary>Free-text filter matched against the excepted wage type code and names.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
