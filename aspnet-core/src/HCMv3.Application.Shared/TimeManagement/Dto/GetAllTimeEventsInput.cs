using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing time events.
/// </summary>
public class GetAllTimeEventsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    public DateTime? TransDateFrom { get; set; }

    public DateTime? TransDateTo { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "TransDate DESC, TransTime DESC";
        }
    }
}
