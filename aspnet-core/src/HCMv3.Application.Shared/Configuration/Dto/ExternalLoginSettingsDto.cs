using System.Collections.Generic;

namespace HCMv3.Configuration.Dto;

public class ExternalLoginSettingsDto
{
    public List<string> EnabledSocialLoginSettings { get; set; } = new List<string>();
}

