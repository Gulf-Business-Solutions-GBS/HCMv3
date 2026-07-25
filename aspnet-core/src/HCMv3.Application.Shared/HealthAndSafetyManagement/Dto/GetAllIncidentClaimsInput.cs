using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing incident claims.
/// </summary>
public class GetAllIncidentClaimsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? IncidentId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ClaimDate DESC";
        }
    }
}
