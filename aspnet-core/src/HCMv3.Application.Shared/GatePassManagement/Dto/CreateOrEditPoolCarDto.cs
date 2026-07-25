using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

public class CreateOrEditPoolCarDto
{
    public int? Id { get; set; }

    [Required]
    public int VehicleTypeId { get; set; }

    [Required]
    [StringLength(64)]
    public string VehiclePlateNumber { get; set; }

    [Required]
    public DateTime VehicleDocExpiryDate { get; set; }

    [Required]
    public int VehicleBrandId { get; set; }

    [Required]
    public int VehicleModelId { get; set; }
}
