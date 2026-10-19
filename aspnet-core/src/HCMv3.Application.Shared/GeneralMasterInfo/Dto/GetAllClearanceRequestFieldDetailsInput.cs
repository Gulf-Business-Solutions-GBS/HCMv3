using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Paged/sorted input for listing clearance request field details (pure detail record: no Code/Name filter).
/// </summary>
public class GetAllClearanceRequestFieldDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>When set, only the details of this clearance request field (its own page's Details tab).</summary>
    public int? ClearanceRequestFieldId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
