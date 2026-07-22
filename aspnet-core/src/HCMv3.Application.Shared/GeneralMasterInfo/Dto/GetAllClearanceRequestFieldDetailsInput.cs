using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Paged/sorted input for listing clearance request field details (pure detail record: no Code/Name filter).
/// </summary>
public class GetAllClearanceRequestFieldDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
