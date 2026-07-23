using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SocialSecurityEducationAllowance"/>.
/// </summary>
public class SocialSecurityEducationAllowanceDto : EntityDto<int>
{
    public int SocialSecurityId { get; set; }

    public int EducationBranchId { get; set; }

    public string EducationBranchName { get; set; }

    public int EducationId { get; set; }

    public string EducationName { get; set; }

    public double Amount { get; set; }
}
