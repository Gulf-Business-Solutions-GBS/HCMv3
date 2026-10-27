using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Analysis By Department grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeAnalysisByDept.ascx</c>).
/// A SUMMARY report: one row per department. See the row DTO for the flagged always-zero
/// <c>TotalValue</c> column.
/// </summary>
public interface ITimeAnalysisByDeptReportAppService : IApplicationService
{
    Task<PagedResultDto<TimeAnalysisByDeptReportRowDto>> GetTimeAnalysisByDeptReport(
        GetTimeAnalysisByDeptReportInput input);

    Task<FileDto> GetTimeAnalysisByDeptReportToExcel(GetTimeAnalysisByDeptReportInput input);
}
