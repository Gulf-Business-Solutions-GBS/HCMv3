using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing work-location duty periods.
/// </summary>
public class GetAllWorkLocationDutyPeriodsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by WorkLocation.
    /// </summary>
    public int? WorkLocationId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
