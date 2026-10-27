using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SocialSecurityMaritalAllowance"/>.
/// </summary>
public class SocialSecurityMaritalAllowanceDto : EntityDto<int>
{
    public int SocialSecurityId { get; set; }

    public int EducationBranchId { get; set; }

    public string EducationBranchName { get; set; }

    public int MaritalStatusId { get; set; }

    public string MaritalStatusName { get; set; }

    public double Amount { get; set; }
}
