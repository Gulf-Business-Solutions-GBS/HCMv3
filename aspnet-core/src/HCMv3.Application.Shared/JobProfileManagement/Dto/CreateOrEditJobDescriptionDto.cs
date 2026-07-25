using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="JobDescription"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditJobDescriptionDto
{
    public int? Id { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    [Required]
    [StringLength(256)]
    public string ShortDescriptionEn { get; set; }

    [Required]
    [StringLength(256)]
    public string ShortDescriptionAr { get; set; }

    [Required]
    public string LongDescriptionEn { get; set; }

    [Required]
    public string LongDescriptionAr { get; set; }

    [StringLength(64)]
    public string Version { get; set; }

    public DateTime VersionChangedOn { get; set; }
}
