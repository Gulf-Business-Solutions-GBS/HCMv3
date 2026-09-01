using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing exit permits.
/// </summary>
public class GetAllEmployeeExitPermitsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by employee.</summary>
    public int? EmployeeId { get; set; }

    public TravelReason? TravelReason { get; set; }

    /// <summary>Inclusive lower bound on <c>ExitDate</c>.</summary>
    public DateTime? ExitDateFrom { get; set; }

    /// <summary>Inclusive upper bound on <c>ExitDate</c>.</summary>
    public DateTime? ExitDateTo { get; set; }

    /// <summary>Free-text filter matched against the permit number.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExitDate DESC";
        }
    }
}
