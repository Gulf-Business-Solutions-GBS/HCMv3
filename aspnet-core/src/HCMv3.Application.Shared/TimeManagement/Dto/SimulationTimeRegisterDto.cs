using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SimulationTimeRegister"/>.
/// </summary>
public class SimulationTimeRegisterDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime DayDate { get; set; }

    public int? DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }
}
