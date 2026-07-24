using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SponsorSignatory"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditSponsorSignatoryDto
{
    public int? Id { get; set; }

    [Required]
    public int SponsorId { get; set; }

    [Required]
    public int SignatoryId { get; set; }
}
