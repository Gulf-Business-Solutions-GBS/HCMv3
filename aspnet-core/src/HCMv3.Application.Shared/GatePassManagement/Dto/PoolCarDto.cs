using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

public class PoolCarDto : EntityDto<int>
{
    public int VehicleTypeId { get; set; }

    public string VehicleTypeNameEn { get; set; }

    public string VehiclePlateNumber { get; set; }

    public DateTime VehicleDocExpiryDate { get; set; }

    public int VehicleBrandId { get; set; }

    public string VehicleBrandNameEn { get; set; }

    public int VehicleModelId { get; set; }

    public string VehicleModelNameEn { get; set; }
}
