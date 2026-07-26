using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SystemSecurity.Dto;

public class GetAllPunchNotificationLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeDutyPeriodId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Date DESC";
        }
    }
}
