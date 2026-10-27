using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Analysis By Department grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysisByDept.ascx</c>).
/// A SUMMARY report: one row per department and period, not one per employee.
/// </summary>
public interface IPayrollAnalysisByDeptReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollAnalysisByDeptReportRowDto>> GetPayrollAnalysisByDeptReport(
        GetPayrollAnalysisByDeptReportInput input);

    Task<FileDto> GetPayrollAnalysisByDeptReportToExcel(GetPayrollAnalysisByDeptReportInput input);
}
