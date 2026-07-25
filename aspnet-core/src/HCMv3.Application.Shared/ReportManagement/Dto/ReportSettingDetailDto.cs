using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportSettingDetail"/>.</summary>
public class ReportSettingDetailDto : EntityDto<int>
{
    public int ReportSettingId { get; set; }

    public string FieldName { get; set; }

    public string CaptionEn { get; set; }

    public string CaptionAr { get; set; }

    public int Order { get; set; }

    public ReportColumnDataType ColumnDataType { get; set; }

    public string DisplayFormat { get; set; }

    public double Width { get; set; }

    public bool VisibleInEn { get; set; }

    public bool VisibleInAr { get; set; }

    public ReportColumnAlignment DataHAlignment { get; set; }
}
