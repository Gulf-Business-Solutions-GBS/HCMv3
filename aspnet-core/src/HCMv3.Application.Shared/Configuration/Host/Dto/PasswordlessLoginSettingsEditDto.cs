namespace HCMv3.Configuration.Host.Dto;

public class PasswordlessLoginSettingsEditDto
{
    public bool IsEnabledForApplication { get; set; }
    public bool IsEmailProviderEnabledForApplication { get; set; }
    public bool IsSmsProviderEnabledForApplication { get; set; }

    public bool IsEmailProviderEnabled { get; set; }

    public bool IsSmsProviderEnabled { get; set; }
}

