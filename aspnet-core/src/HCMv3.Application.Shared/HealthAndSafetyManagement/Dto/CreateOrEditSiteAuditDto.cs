using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SiteAudit"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditSiteAuditDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime AuditDate { get; set; }

    public DateTime AuditTime { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    public int ConductedById { get; set; }

    public string Comments { get; set; }

    [Required]
    public DateTime ReassessmentDate { get; set; }
}
