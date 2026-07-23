using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence violation action logs.
/// </summary>
public class GetAllAbsenceViolationActionLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent AbsenceViolationActionRecord.
    /// </summary>
    public int? AbsenceViolationActionRecordId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Step ASC";
        }
    }
}
