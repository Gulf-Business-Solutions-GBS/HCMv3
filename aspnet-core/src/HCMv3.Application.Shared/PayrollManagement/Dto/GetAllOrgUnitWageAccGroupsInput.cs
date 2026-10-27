using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing org-unit wage account groups.
/// </summary>
public class GetAllOrgUnitWageAccGroupsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public long? OrganizationUnitId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate ASC";
        }
    }
}
