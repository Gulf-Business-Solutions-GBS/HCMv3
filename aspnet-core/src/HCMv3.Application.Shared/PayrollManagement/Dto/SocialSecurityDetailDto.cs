using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SocialSecurityDetail"/>.
/// </summary>
public class SocialSecurityDetailDto : EntityDto<int>
{
    public int SocialSecurityId { get; set; }

    public double MinEarnings { get; set; }

    public double MaxEarnings { get; set; }

    public double EmployeeContribution { get; set; }

    public double EmployerContribution { get; set; }

    public double MinEarningsEmployeeContribution { get; set; }
}
