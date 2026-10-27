using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class GetAllRequestDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? RequestMasterId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Step";
        }
    }
}

/// <summary>
/// The chain's steps in their new order.
///
/// <para>
/// Sent as the complete list rather than as "move id X to position N", because the grid already knows the
/// whole order after a drag and a partial instruction has to be replayed against a server state that may
/// have moved. The service rejects a list that is not exactly this chain's steps, which is also what
/// catches a stale grid.
/// </para>
/// </summary>
public class ReorderRequestDetailsInput
{
    [Range(1, int.MaxValue)]
    public int RequestMasterId { get; set; }

    [Required]
    public List<int> OrderedIds { get; set; } = new List<int>();
}
