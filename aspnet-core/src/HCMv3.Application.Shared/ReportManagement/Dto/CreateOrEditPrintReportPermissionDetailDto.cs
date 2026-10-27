using System.ComponentModel.DataAnnotations;

namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PrintReportPermissionDetail"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditPrintReportPermissionDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int PrintReportPermissionId { get; set; }

    [Required]
    public int PrintReportId { get; set; }
}
