using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee survey assignments.
/// </summary>
public class GetAllEmployeeSurveysInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Optional filter by Survey.
    /// </summary>
    public int? SurveyId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
