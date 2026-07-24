using System.ComponentModel.DataAnnotations;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ProjectRegisterDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditProjectRegisterDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int ProjectRegisterId { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
