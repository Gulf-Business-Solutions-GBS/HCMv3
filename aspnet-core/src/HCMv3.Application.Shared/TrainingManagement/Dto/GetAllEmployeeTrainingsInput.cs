using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee training records.
/// </summary>
public class GetAllEmployeeTrainingsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Optional filter by TrainingSchedule.
    /// </summary>
    public int? TrainingScheduleId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
