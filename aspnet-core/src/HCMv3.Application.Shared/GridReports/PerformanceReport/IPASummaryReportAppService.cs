using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// PA Summary analysis grid report (port of legacy
/// <c>PerformanceReport/uscRptPASummary.ascx</c>).
/// </summary>
public interface IPASummaryReportAppService : IApplicationService
{
    Task<PagedResultDto<PASummaryReportRowDto>> GetPASummaryReport(GetPASummaryReportInput input);

    Task<FileDto> GetPASummaryReportToExcel(GetPASummaryReportInput input);
}
