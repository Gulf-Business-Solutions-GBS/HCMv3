using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Main Format-New" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>, payroll-area-Name-contains-"MYB" column set
/// (lines 1578-2016, <c>IsDetail == false</c>).
/// </summary>
public interface IPayrollJVCustom016MainFormatMybReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016MainFormatMybRowDto>> GetPayrollJVCustom016MainFormatMybReport(
        GetPayrollJVCustom016MainFormatReportInput input);

    Task<FileDto> GetPayrollJVCustom016MainFormatMybReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input);
}
