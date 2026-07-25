using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing onboarding applicant tasks.
/// </summary>
public class GetAllOnbApplicantTasksInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent Applicant.
    /// </summary>
    public int? ApplicantId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
