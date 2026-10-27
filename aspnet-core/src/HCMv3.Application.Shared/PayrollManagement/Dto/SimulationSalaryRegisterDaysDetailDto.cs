using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SimulationSalaryRegisterDaysDetail"/>.
/// </summary>
public class SimulationSalaryRegisterDaysDetailDto : EntityDto<int>
{
    public int SimulationSalaryRegisterDetailId { get; set; }

    public DateTime Period { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
