using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GatePassManagement.Dto;

public class GetAllEmployeePoolCarsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? DriverEmployeeId { get; set; }

    public int? PoolCarId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
