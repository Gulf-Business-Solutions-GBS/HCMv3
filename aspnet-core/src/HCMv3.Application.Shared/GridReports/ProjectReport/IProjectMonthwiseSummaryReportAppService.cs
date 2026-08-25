using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.ProjectReport.Dto;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Monthwise Summary analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectMonthwiseSummary.ascx</c>).
/// ATYPICAL: rows carry a dynamic per-project pivot -- see <see cref="Dto.ProjectMonthwiseSummaryReportRowDto"/>.
/// </summary>
public interface IProjectMonthwiseSummaryReportAppService : IApplicationService
{
    Task<PagedResultDto<ProjectMonthwiseSummaryReportRowDto>> GetProjectMonthwiseSummaryReport(
        GetProjectMonthwiseSummaryReportInput input);

    Task<FileDto> GetProjectMonthwiseSummaryReportToExcel(GetProjectMonthwiseSummaryReportInput input);
}
