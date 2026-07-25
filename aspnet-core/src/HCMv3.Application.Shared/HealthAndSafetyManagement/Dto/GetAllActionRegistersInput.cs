using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing action register entries.
/// </summary>
public class GetAllActionRegistersInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? IncidentId { get; set; }

    public int? RiskId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ActionDate DESC";
        }
    }
}
