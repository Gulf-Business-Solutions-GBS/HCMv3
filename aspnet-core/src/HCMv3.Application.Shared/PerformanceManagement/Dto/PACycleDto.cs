using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PACycleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsDefault { get; set; }
}
