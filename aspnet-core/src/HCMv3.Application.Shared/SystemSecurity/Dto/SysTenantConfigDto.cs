using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class SysTenantConfigDto : EntityDto<int>
{
    public string Key { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string KeyValue { get; set; }
    public TenantConfigCategory ConfigCategory { get; set; }
    public string ConfigDescription { get; set; }
}
