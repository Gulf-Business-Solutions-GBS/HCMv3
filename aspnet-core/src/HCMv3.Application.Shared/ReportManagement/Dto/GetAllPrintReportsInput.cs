using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Paged/sorted/filtered input for listing print reports.</summary>
public class GetAllPrintReportsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Free-text filter matched against Code / NameEn / NameAr.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
