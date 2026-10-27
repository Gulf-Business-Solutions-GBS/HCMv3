using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SalaryRegisterDetail"/>.
/// </summary>
public class SalaryRegisterDetailDto : EntityDto<int>
{
    public int SalaryRegisterId { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
