using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Pre-Payroll Report grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPrePayrollReport.ascx</c>): a per-employee
/// payroll-readiness checklist.
/// </summary>
public interface IPrePayrollReportAppService : IApplicationService
{
    Task<PagedResultDto<PrePayrollReportRowDto>> GetPrePayrollReport(GetPrePayrollReportInput input);

    Task<FileDto> GetPrePayrollReportToExcel(GetPrePayrollReportInput input);
}
