using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class KpiCategoryDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string HexColor { get; set; }
}
