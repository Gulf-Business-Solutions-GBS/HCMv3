using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SystemSecurity.Dto;

public class GetAllBulkUploadLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public BulkUploadLogType? LogType { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RunTimeStamp DESC";
        }
    }
}
