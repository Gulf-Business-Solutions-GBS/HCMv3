using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ProjectRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditProjectRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime Period { get; set; }
}
