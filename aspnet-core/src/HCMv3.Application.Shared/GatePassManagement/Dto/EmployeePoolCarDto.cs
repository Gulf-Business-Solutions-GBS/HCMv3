using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

public class EmployeePoolCarDto : EntityDto<int>
{
    public int DriverEmployeeId { get; set; }

    public string DriverFullName { get; set; }

    public int PoolCarId { get; set; }

    public string PoolCarPlateNumber { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsReturned { get; set; }
}
