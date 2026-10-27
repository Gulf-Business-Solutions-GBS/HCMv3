using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OrganizationDocument"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditOrganizationDocumentDto
{
    public int? Id { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public int DocumentTypeId { get; set; }

    /// <summary>"Document Number" in the legacy UI.</summary>
    [StringLength(256)]
    public string DocumentNum { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    [StringLength(256)]
    public string DocumentPlace { get; set; }

    public bool NotificationSent { get; set; }

    public int? SponsorId { get; set; }
}
