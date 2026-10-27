using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Detail" analysis grid report (SuperAdmin-only, legacy line 250-256) -- port of
/// legacy <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>, BMC payroll-area column set
/// (lines 1176-1445 + 1527-1572, <c>IsDetail == true</c>).
/// </summary>
public interface IPayrollJVCustom016DetailReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016DetailRowDto>> GetPayrollJVCustom016DetailReport(
        GetPayrollJVCustom016MainFormatReportInput input);

    Task<FileDto> GetPayrollJVCustom016DetailReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input);
}
