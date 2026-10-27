using Abp.Auditing;
using HCMv3.Configuration.Dto;

namespace HCMv3.Configuration.Tenants.Dto;

public class TenantEmailSettingsEditDto : EmailSettingsEditDto
{
    public bool UseHostDefaultEmailSettings { get; set; }
}

