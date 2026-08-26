using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Main Format" (and, for BMC, identically "Main Format-New") analysis grid
/// report -- port of legacy <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>, BMC payroll-area
/// column set (lines 1176-1524, <c>IsDetail == false</c>).
/// </summary>
public interface IPayrollJVCustom016MainFormatBmcReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016MainFormatBmcRowDto>> GetPayrollJVCustom016MainFormatBmcReport(
        GetPayrollJVCustom016MainFormatReportInput input);

    Task<FileDto> GetPayrollJVCustom016MainFormatBmcReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input);
}
