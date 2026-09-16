using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing wage type leaves.
/// </summary>
public class GetAllWageTypeLeavesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WageTypeId { get; set; }

    public int? AbsenceTypeId { get; set; }

    /// <summary>Free-text filter matched against the wage type and absence type codes and names.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
