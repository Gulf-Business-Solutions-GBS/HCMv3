using System.ComponentModel.DataAnnotations;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="JobDescriptionAcknowledgement"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditJobDescriptionAcknowledgementDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    [Required]
    public int JobDescriptionId { get; set; }

    [StringLength(64)]
    public string Version { get; set; }

    public bool Acknowledged { get; set; }
}
