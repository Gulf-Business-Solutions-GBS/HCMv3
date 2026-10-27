using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="PrintReportPermissionDetail"/>.</summary>
public class PrintReportPermissionDetailDto : EntityDto<int>
{
    public int PrintReportPermissionId { get; set; }

    public int PrintReportId { get; set; }

    public string PrintReportName { get; set; }
}
