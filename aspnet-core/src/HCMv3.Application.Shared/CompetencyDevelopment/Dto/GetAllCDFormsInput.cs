using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Paged/sorted/filtered input for listing CD forms.</summary>
public class GetAllCDFormsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Optional filter by CDPeriod.</summary>
    public int? CDPeriodId { get; set; }

    /// <summary>Optional filter by Employee.</summary>
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
