using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Custom019 Monthly Attendance With Tolerance analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance_Custom019.ascx</c>).
///
/// A SEPARATE customer implementation, not a parameterisation of
/// <see cref="IMonthlyAttWithToleranceReportAppService"/>: it replaces the base's DA/MA/Deduction model
/// with a LateIn+EarlyOut daily deduction, adds a per-employee monthly deduction row, adds three org
/// filters and a Detail/Summary variant, and drops the mandatory-employee-code rule.
/// </summary>
public interface IMonthlyAttWithToleranceCustom019ReportAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyAttWithToleranceCustom019ReportRowDto>> GetMonthlyAttWithToleranceCustom019Report(
        GetMonthlyAttWithToleranceCustom019ReportInput input);

    Task<FileDto> GetMonthlyAttWithToleranceCustom019ReportToExcel(
        GetMonthlyAttWithToleranceCustom019ReportInput input);
}
