using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WageTypeSecurityRole"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditWageTypeSecurityRoleDto
{
    public int? Id { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    [Required]
    public int RoleId { get; set; }
}
