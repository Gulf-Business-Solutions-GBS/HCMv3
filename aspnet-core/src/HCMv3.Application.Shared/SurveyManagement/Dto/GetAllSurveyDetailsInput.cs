using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing survey questions.
/// </summary>
public class GetAllSurveyDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent Survey.
    /// </summary>
    public int? SurveyId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "OrderNo";
        }
    }
}
