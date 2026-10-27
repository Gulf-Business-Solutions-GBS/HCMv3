using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing accommodation services.
/// </summary>
public class GetAllAccommodationServicesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Optional filter by parent EmployeeAccommodation.
    /// </summary>
    public int? EmployeeAccommodationId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
