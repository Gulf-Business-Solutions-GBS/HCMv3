using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

public class CreateOrEditEmployeePoolCarDto
{
    public int? Id { get; set; }

    [Required]
    public int DriverEmployeeId { get; set; }

    [Required]
    public int PoolCarId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsReturned { get; set; }
}
