using Abp.Configuration;

namespace HCMv3.Timing.Dto;

public class GetTimezonesInput
{
    public SettingScopes DefaultTimezoneScope { get; set; }
}

