using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.OrganizationManagement.Dto;

public class GetAllOrgUnitsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            // "Code" here is Zero's own OrganizationUnit.Code (hierarchical tree-position code),
            // applied before the profile join/projection -- gives natural tree order.
            Sorting = "Code ASC";
        }
    }
}
