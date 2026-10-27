using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee survey answers.
/// </summary>
public class GetAllEmployeeSurveyDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeSurvey.
    /// </summary>
    public int? EmployeeSurveyId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
