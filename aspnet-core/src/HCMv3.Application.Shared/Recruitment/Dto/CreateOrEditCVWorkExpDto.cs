using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CVWorkExp"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCVWorkExpDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantCVId { get; set; }

    [StringLength(256)]
    public string Company { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(256)]
    public string Position { get; set; }

    public string Responsibilities { get; set; }
}
