using Abp.Application.Services.Dto;

namespace HCMv3.Maui.Models.Common;

public class TenantLoginInfoPersistanceModel : EntityDto
{
    public string TenancyName { get; set; }

    public string Name { get; set; }
}