using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence violation action records.
/// </summary>
public class GetAllAbsenceViolationActionRecordsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
