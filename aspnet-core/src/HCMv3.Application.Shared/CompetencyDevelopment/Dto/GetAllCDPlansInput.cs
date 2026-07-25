using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing CD plan rows.</summary>
public class GetAllCDPlansInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by parent CDForm.</summary>
    public int? CDFormId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
