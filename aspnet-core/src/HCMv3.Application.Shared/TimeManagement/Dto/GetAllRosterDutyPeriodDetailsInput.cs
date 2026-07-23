using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing roster duty period details.
/// </summary>
public class GetAllRosterDutyPeriodDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent RosterDutyPeriod.
    /// </summary>
    public int? RosterDutyPeriodId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Week ASC";
        }
    }
}
