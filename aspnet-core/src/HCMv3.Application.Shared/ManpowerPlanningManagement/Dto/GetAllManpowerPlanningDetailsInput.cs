using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing manpower planning details of a header.
/// </summary>
public class GetAllManpowerPlanningDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? ManpowerPlanningId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id";
        }
    }
}
