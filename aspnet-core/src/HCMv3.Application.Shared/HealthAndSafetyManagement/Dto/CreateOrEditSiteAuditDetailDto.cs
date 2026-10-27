using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SiteAuditDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSiteAuditDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SiteAuditId { get; set; }

    [Required]
    public string Observation { get; set; }

    public string CorrectionAction { get; set; }
}
