using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Paged/sorted/filtered input for listing report infos.</summary>
public class GetAllReportInfosInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Free-text filter matched against ReportName.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ReportName ASC";
        }
    }
}
