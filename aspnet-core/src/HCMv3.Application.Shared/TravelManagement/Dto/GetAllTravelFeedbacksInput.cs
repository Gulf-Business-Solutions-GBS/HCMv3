using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing travel feedback/claim records.
/// </summary>
public class GetAllTravelFeedbacksInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ClaimDate DESC";
        }
    }
}
