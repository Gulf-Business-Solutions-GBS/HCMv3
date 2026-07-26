using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class SysUserOrgUnitDataDto : EntityDto<int>
{
    public int? EmployeeId { get; set; }
    public string EmployeeFullName { get; set; }
    public int RoleId { get; set; }
    public string RoleDisplayName { get; set; }
    public int OrgLevelId { get; set; }
    public string OrgLevelNameEn { get; set; }
    public long OrganizationUnitId { get; set; }
    public string OrganizationUnitDisplayName { get; set; }
    public bool ShowUnlocked { get; set; }
}
