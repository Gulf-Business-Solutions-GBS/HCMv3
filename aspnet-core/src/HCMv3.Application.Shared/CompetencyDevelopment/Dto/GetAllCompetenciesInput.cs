using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing competencies.</summary>
public class GetAllCompetenciesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Free-text filter matched against Code / NameEn / NameAr.</summary>
    public string Filter { get; set; }

    /// <summary>Optional filter by CompetencyType.</summary>
    public int? CompetencyTypeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
