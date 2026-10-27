using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportType"/>.</summary>
public class ReportTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }
}
