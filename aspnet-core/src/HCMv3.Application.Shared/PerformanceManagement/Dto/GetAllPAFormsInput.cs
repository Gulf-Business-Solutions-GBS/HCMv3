using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllPAFormsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PAPeriodId { get; set; }

    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Id ASC";
        }
    }
}
