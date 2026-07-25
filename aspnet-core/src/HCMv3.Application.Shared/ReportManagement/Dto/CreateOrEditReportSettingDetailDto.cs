using System.ComponentModel.DataAnnotations;

namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ReportSettingDetail"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditReportSettingDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int ReportSettingId { get; set; }

    [Required]
    [StringLength(256)]
    public string FieldName { get; set; }

    [StringLength(256)]
    public string CaptionEn { get; set; }

    [StringLength(256)]
    public string CaptionAr { get; set; }

    public int Order { get; set; }

    public ReportColumnDataType ColumnDataType { get; set; }

    [StringLength(50)]
    public string DisplayFormat { get; set; }

    public double Width { get; set; }

    public bool VisibleInEn { get; set; }

    public bool VisibleInAr { get; set; }

    public ReportColumnAlignment DataHAlignment { get; set; }
}
