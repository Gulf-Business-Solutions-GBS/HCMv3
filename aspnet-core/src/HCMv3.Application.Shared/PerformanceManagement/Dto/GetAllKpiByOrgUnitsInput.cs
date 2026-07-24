using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllKpiByOrgUnitsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public long? OrganizationUnitId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "StartDate ASC";
        }
    }
}
