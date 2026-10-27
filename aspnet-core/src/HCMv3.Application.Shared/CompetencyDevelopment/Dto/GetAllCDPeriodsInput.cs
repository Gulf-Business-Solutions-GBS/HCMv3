using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing CD periods.</summary>
public class GetAllCDPeriodsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Free-text filter matched against Name.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Name ASC";
        }
    }
}
