using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SystemSecurity.Dto;

public class GetAllExecutionLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public ExecutionLogModule? Module { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ActionDateTime DESC";
        }
    }
}
