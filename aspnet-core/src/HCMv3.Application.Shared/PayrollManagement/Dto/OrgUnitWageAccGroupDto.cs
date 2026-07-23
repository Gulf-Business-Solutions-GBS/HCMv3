using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="OrgUnitWageAccGroup"/>.
/// </summary>
public class OrgUnitWageAccGroupDto : EntityDto<int>
{
    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int WageAccGroupId { get; set; }

    public string WageAccGroupName { get; set; }
}
