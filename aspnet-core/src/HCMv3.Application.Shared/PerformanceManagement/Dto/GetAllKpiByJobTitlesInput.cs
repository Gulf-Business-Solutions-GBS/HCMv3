using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllKpiByJobTitlesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? JobTitleId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "StartDate ASC";
        }
    }
}
