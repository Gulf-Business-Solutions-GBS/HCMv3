using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupFund"/>.
/// </summary>
public class EmployeeSubGroupFundDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public double FromPay { get; set; }

    public double ToPay { get; set; }

    public FundDedType FundDedType { get; set; }

    public double Percentage { get; set; }

    public double MinAmount { get; set; }

    public double MaxAmount { get; set; }

    public double FixAmount { get; set; }
}
