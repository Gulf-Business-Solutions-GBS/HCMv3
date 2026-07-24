using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SponsorGovtRepresentative"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSponsorGovtRepresentativeDto
{
    public int? Id { get; set; }

    [Required]
    public int SponsorId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
