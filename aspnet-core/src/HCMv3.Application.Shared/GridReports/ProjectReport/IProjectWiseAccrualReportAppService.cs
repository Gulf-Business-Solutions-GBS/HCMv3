using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.ProjectReport.Dto;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Wise Accrual analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectWiseAccrual.ascx</c>).
/// </summary>
public interface IProjectWiseAccrualReportAppService : IApplicationService
{
    Task<PagedResultDto<ProjectWiseAccrualReportRowDto>> GetProjectWiseAccrualReport(
        GetProjectWiseAccrualReportInput input);

    Task<FileDto> GetProjectWiseAccrualReportToExcel(GetProjectWiseAccrualReportInput input);
}
