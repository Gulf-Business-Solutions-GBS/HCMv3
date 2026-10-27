using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PARatingDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsCommentRequired { get; set; }

    public bool Status { get; set; }

    public int MaxLimit { get; set; }

    public string DescriptionEn { get; set; }

    public string DescriptionAr { get; set; }

    public double FromRange { get; set; }

    public double ToRange { get; set; }
}
