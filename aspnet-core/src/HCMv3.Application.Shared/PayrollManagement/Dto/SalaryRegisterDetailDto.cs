using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SalaryRegisterDetail"/>.
/// </summary>
public class SalaryRegisterDetailDto : EntityDto<int>
{
    public int SalaryRegisterId { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public int PayrollOrder { get; set; }

    /// <summary>The wage type's billing type; with <see cref="DontImpactNetPay"/> it colours the line (legacy parity).</summary>
    public BillingType BillingType { get; set; }

    public bool DontImpactNetPay { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
