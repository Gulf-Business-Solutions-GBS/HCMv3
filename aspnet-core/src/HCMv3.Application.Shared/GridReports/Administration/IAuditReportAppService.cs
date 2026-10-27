using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Administration.Dto;

namespace HCMv3.GridReports.Administration;

/// <summary>
/// Audit Report analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Administration/uscRptAuditReport.ascx</c>).
/// </summary>
public interface IAuditReportAppService : IApplicationService
{
    Task<PagedResultDto<AuditReportRowDto>> GetAuditReport(GetAuditReportInput input);

    Task<FileDto> GetAuditReportToExcel(GetAuditReportInput input);
}
