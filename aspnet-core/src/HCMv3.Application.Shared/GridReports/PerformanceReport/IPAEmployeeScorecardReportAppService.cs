using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// PA Employee Scorecard analysis grid report (port of legacy customer variant
/// <c>PerformanceReport/uscRptPAEmployee_Custom033.ascx</c>).
/// </summary>
public interface IPAEmployeeScorecardReportAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeScorecardReportRowDto>> GetPAEmployeeScorecardReport(GetPAEmployeeScorecardReportInput input);

    Task<FileDto> GetPAEmployeeScorecardReportToExcel(GetPAEmployeeScorecardReportInput input);
}
