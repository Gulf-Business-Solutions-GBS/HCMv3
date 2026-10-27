using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Paged/sorted/filtered input for listing duties.
/// </summary>
public class GetAllDutiesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>
    /// Free-text filter matched against Code / NameEn / NameAr.
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Optional: only active (true) or only inactive (false) duties. Duty pickers send true so an inactive
    /// duty is no longer offered; the Duties screen itself sends nothing and lists every duty.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Duties always returned whatever <see cref="IsActive"/> says: a picker passes the duties already on
    /// the record it edits, so a duty made inactive later still shows as the current choice.
    /// </summary>
    public List<int> IncludeIds { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
