using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing site audits.
/// </summary>
public class GetAllSiteAuditsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? LocationId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "AuditDate DESC";
        }
    }
}
