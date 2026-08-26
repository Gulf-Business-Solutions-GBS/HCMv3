using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Leave Provision" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>, <c>cmbType == "Leave Provision"</c> branch
/// (lines 2397-2456).
/// </summary>
public interface IPayrollJVCustom016LeaveProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016ProvisionDiffRowDto>> GetPayrollJVCustom016LeaveProvisionReport(
        GetPayrollJVCustom016ProvisionDiffReportInput input);

    Task<FileDto> GetPayrollJVCustom016LeaveProvisionReportToExcel(
        GetPayrollJVCustom016ProvisionDiffReportInput input);
}
