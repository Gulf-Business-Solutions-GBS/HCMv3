using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing training courses.
/// </summary>
public class GetAllTrainingCoursesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "NameEn";
        }
    }
}
