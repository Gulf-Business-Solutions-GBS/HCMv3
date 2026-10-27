using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class KpiInfoDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int KpiCategoryId { get; set; }

    public string KpiCategoryName { get; set; }

    public int? CompetencyId { get; set; }

    public string CompetencyNameEn { get; set; }

    public string Comments { get; set; }

    public bool Active { get; set; }
}
