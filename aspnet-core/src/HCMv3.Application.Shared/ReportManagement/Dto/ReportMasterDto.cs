using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportMaster"/>.</summary>
public class ReportMasterDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
