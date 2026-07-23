using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SimulationSalaryRegisterDetail"/>.
/// </summary>
public class SimulationSalaryRegisterDetailDto : EntityDto<int>
{
    public int SimulationSalaryRegisterId { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
