using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditSysUserOrgUnitDataDto
{
    public int? Id { get; set; }

    public int? EmployeeId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int OrgLevelId { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    public bool ShowUnlocked { get; set; }
}
