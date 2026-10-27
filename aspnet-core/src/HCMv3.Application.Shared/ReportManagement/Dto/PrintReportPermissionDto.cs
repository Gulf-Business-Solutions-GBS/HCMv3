using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="PrintReportPermission"/>.</summary>
public class PrintReportPermissionDto : EntityDto<int>
{
    public string Code { get; set; }

    public int RoleId { get; set; }

    public string RoleName { get; set; }
}
