using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.PerformanceManagement.Dto;

public class GetAllPAEmployeesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? PACycleId { get; set; }

    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Id ASC";
        }
    }
}
