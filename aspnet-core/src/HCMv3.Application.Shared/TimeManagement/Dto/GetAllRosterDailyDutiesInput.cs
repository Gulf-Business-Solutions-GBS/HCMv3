using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing roster daily duties.
/// </summary>
public class GetAllRosterDailyDutiesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent RosterDutySchedule.
    /// </summary>
    public int? RosterDutyScheduleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "CalendarDate ASC";
        }
    }
}
