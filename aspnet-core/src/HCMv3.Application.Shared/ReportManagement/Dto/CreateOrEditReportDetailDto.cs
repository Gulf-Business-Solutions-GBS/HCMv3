using System.ComponentModel.DataAnnotations;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Create/Update input DTO for <see cref="ReportDetail"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditReportDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int ReportMasterId { get; set; }

    public int? ReportSettingId { get; set; }

    public int DrillDownLevel { get; set; }
}
