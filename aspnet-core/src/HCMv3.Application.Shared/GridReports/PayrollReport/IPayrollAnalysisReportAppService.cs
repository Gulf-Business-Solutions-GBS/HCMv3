using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysis.ascx</c>).
/// A SUMMARY report: one row per top-level org unit and period, not one per employee.
/// </summary>
public interface IPayrollAnalysisReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollAnalysisReportRowDto>> GetPayrollAnalysisReport(
        GetPayrollAnalysisReportInput input);

    Task<FileDto> GetPayrollAnalysisReportToExcel(GetPayrollAnalysisReportInput input);
}
