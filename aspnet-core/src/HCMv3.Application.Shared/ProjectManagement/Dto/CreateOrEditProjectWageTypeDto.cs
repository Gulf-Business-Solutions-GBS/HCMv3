using System.ComponentModel.DataAnnotations;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ProjectWageType"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditProjectWageTypeDto
{
    public int? Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int WageTypeId { get; set; }
}
