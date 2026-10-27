using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing training schedules.
/// </summary>
public class GetAllTrainingSchedulesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    /// <summary>
    /// Optional filter by parent TrainingCourse.
    /// </summary>
    public int? TrainingCourseId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn";
        }
    }
}
