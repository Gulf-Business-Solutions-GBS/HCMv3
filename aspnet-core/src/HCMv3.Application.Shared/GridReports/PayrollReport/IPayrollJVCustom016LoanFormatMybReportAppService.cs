using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Loan Format - MYB" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// <c>cmbType == "Loan Format"</c> branch, MYB payroll-area column set (lines 2194-2298).
/// </summary>
public interface IPayrollJVCustom016LoanFormatMybReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016LoanFormatMybRowDto>> GetPayrollJVCustom016LoanFormatMybReport(
        GetPayrollJVCustom016LoanFormatReportInput input);

    Task<FileDto> GetPayrollJVCustom016LoanFormatMybReportToExcel(
        GetPayrollJVCustom016LoanFormatReportInput input);
}
