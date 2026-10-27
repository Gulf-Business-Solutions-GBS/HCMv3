using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing training attendance rows.
/// </summary>
public class GetAllTrainingAttendancesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent TrainingSchedule.
    /// </summary>
    public int? TrainingScheduleId { get; set; }

    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "DayDate DESC";
        }
    }
}
