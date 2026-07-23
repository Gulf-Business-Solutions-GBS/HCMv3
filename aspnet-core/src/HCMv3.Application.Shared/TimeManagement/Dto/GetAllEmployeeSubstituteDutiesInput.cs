using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing employee substitute duties.
/// </summary>
public class GetAllEmployeeSubstituteDutiesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by Employee.
    /// </summary>
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "SubstituteDate DESC";
        }
    }
}
