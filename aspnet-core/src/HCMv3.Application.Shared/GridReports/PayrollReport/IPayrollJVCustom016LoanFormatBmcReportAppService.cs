using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Loan Format - BMC" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// <c>cmbType == "Loan Format"</c> branch, BMC payroll-area column set (lines 2091-2191).
/// </summary>
public interface IPayrollJVCustom016LoanFormatBmcReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016LoanFormatBmcRowDto>> GetPayrollJVCustom016LoanFormatBmcReport(
        GetPayrollJVCustom016LoanFormatReportInput input);

    Task<FileDto> GetPayrollJVCustom016LoanFormatBmcReportToExcel(
        GetPayrollJVCustom016LoanFormatReportInput input);
}
