namespace HCMv3.Configuration.Host.Dto;

public class CaptchaSettingsEditDto
{
    public bool UseCaptchaOnRegistration { get; set; }

    public bool UseCaptchaOnEmailActivation { get; set; }

    public bool UseCaptchaOnResetPassword { get; set; }

    public bool UseCaptchaOnLogin { get; set; }
}

