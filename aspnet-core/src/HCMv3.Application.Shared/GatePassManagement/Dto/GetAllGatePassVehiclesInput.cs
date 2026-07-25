using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GatePassManagement.Dto;

public class GetAllGatePassVehiclesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? GatePassId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "VehiclePlateNumber ASC";
        }
    }
}
