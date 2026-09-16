using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing wage type security roles.
/// </summary>
public class GetAllWageTypeSecurityRolesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? WageTypeId { get; set; }

    /// <summary>Free-text filter matched against the role name.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
