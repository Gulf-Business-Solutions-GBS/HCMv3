using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GatePassManagement.Dto;

public class GetAllGatePassesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public int? GatePassTypeId { get; set; }

    public int? RequestedByEmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "IssueDate DESC";
        }
    }
}
