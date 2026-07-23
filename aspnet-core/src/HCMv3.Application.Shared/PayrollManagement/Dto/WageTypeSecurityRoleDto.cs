using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageTypeSecurityRole"/>.
/// </summary>
public class WageTypeSecurityRoleDto : EntityDto<int>
{
    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public int RoleId { get; set; }

    public string RoleName { get; set; }
}
