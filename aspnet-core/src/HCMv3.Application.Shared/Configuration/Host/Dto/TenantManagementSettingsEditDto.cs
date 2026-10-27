namespace HCMv3.Configuration.Host.Dto;

public class TenantManagementSettingsEditDto
{
    public bool AllowSelfRegistration { get; set; }

    public bool IsNewRegisteredTenantActiveByDefault { get; set; }

    public int? DefaultEditionId { get; set; }

    public CaptchaSettingsEditDto CaptchaSettings { get; set; }

    public bool IsRestrictedEmailDomainEnabled { get; set; }

}

