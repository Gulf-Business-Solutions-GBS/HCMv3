using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Monthly Attendance With Tolerance grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance.ascx</c>).
///
/// One row per employee per calendar day of the selected month, RECOMPUTED from raw time events rather
/// than read from <c>TimeRegister</c>. An employee code is mandatory -- see the input DTO.
///
/// The <c>_Custom019</c> variant of this legacy screen is deliberately OUT OF SCOPE of this port.
/// </summary>
public interface IMonthlyAttWithToleranceReportAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyAttWithToleranceReportRowDto>> GetMonthlyAttWithToleranceReport(
        GetMonthlyAttWithToleranceReportInput input);

    Task<FileDto> GetMonthlyAttWithToleranceReportToExcel(GetMonthlyAttWithToleranceReportInput input);
}
