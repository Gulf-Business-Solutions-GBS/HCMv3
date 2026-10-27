using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Paged/sorted/filtered input for listing report setting details.</summary>
public class GetAllReportSettingDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by parent ReportSetting.</summary>
    public int? ReportSettingId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Order ASC";
        }
    }
}
