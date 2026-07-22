using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="OrganizationUnitType"/>.
/// </summary>
public class OrganizationUnitTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool AllowLeaveConflict { get; set; }

    public bool AssignPayrollArea { get; set; }
}
