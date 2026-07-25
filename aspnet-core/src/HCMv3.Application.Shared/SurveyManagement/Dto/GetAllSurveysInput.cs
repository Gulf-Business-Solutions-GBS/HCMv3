using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing surveys.
/// </summary>
public class GetAllSurveysInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
