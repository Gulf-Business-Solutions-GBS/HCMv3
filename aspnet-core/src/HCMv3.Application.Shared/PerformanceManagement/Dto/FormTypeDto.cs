using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class FormTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int MinReviewerLimit { get; set; }

    public int MaxReviewerLimit { get; set; }
}
