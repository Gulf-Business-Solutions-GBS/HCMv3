using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing absence type exceptions.
/// </summary>
public class GetAllAbsenceTypeExceptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent AbsenceType.
    /// </summary>
    public int? AbsenceTypeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
