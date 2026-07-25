using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ApplicantCV"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditApplicantCVDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantId { get; set; }

    public int? JobTitleId { get; set; }

    [StringLength(256)]
    public string CVName { get; set; }

    public DateTime UploadDate { get; set; }
}
