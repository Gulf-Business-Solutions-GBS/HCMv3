using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing payroll control records.
/// </summary>
public class GetAllPayrollControlRecordsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PayrollAreaId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "CalendarMonthStart ASC";
        }
    }
}
