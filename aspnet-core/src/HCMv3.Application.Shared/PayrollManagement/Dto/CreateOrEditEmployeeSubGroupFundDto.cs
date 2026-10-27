using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupFund"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupFundDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    public double FromPay { get; set; }

    public double ToPay { get; set; }

    public FundDedType FundDedType { get; set; }

    public double Percentage { get; set; }

    public double MinAmount { get; set; }

    public double MaxAmount { get; set; }

    public double FixAmount { get; set; }
}
