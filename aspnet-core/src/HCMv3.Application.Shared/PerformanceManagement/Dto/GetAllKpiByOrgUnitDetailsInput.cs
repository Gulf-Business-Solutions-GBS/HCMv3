using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllKpiByOrgUnitDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? KpiByOrgUnitId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Id ASC";
        }
    }
}
