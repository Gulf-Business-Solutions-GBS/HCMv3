using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="IndemnityPayRuleDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditIndemnityPayRuleDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int IndemnityPayRuleId { get; set; }

    public double MinYearOfService { get; set; }

    public double MaxYearOfService { get; set; }

    public double Number { get; set; }

    public Unit Unit { get; set; }

    public double PayPercentage { get; set; }
}
