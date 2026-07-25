using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GatePassManagement.Dto;

public class GetAllPoolCarsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "VehiclePlateNumber ASC";
        }
    }
}
