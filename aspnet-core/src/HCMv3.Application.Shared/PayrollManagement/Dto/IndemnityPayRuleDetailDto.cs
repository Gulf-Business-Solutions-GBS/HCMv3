using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="IndemnityPayRuleDetail"/>.
/// </summary>
public class IndemnityPayRuleDetailDto : EntityDto<int>
{
    public int IndemnityPayRuleId { get; set; }

    public double MinYearOfService { get; set; }

    public double MaxYearOfService { get; set; }

    public double Number { get; set; }

    public Unit Unit { get; set; }

    public double PayPercentage { get; set; }
}
