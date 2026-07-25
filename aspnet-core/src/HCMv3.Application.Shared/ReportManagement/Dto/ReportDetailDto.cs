using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportDetail"/>.</summary>
public class ReportDetailDto : EntityDto<int>
{
    public int ReportMasterId { get; set; }

    public string ReportMasterName { get; set; }

    public int? ReportSettingId { get; set; }

    public string ReportSettingName { get; set; }

    public int DrillDownLevel { get; set; }
}
