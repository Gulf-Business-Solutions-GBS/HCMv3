using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllPATimelineExceptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PAPeriodId { get; set; }

    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExtensionDate ASC";
        }
    }
}
