using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllPAGoalsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PAFormId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Id ASC";
        }
    }
}
