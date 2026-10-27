using System.ComponentModel.DataAnnotations;

namespace HCMv3.Configuration.Host.Dto;

public class SessionManagementSettingsEditDto
{
    public bool IsEnabled { get; set; }

    public bool IsSessionFingerprintValidationEnabled { get; set; }

    public string SessionFingerprintValidationPolicy { get; set; }

    [Range(10, 43200)]
    public int SessionAbsoluteTimeoutMinutes { get; set; }

    public bool IsSessionRevocationEnabled { get; set; }
}
