using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing yearly time register details.
/// </summary>
public class GetAllYearlyTimeRegisterDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent YearlyTimeRegister.
    /// </summary>
    public int? YearlyTimeRegisterId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
