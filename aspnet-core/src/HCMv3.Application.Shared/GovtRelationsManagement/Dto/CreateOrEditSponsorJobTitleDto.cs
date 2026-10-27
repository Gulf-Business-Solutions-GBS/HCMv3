using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SponsorJobTitle"/>. <c>Id == null</c> means create; otherwise
/// update. <c>CitizenQuotaAccrued</c>/<c>NonCitizenQuotaAccrued</c> are NOT part of the input -- they are
/// always server-computed (see <see cref="SponsorJobTitle"/> remarks).
/// </summary>
public class CreateOrEditSponsorJobTitleDto
{
    public int? Id { get; set; }

    [Required]
    public int SponsorId { get; set; }

    [Required]
    public int GovtJobTitleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int NoofPosition { get; set; }

    [Range(0, 100)]
    public int CitizenPercent { get; set; }

    public int CitizenQuotaUsed { get; set; }

    [Range(0, 100)]
    public int NonCitizenPercent { get; set; }

    public int NonCitizenQuotaUsed { get; set; }
}
