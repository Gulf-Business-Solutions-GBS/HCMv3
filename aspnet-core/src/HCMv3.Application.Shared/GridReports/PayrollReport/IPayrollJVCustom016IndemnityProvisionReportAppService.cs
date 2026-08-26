using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Indemnity Provision" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom016.ascx(.cs)</c>, <c>Table1()</c>, <c>cmbType == "Indemnity Provision"</c>
/// branch (lines 2459-2518).
/// </summary>
public interface IPayrollJVCustom016IndemnityProvisionReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollJVCustom016ProvisionDiffRowDto>> GetPayrollJVCustom016IndemnityProvisionReport(
        GetPayrollJVCustom016ProvisionDiffReportInput input);

    Task<FileDto> GetPayrollJVCustom016IndemnityProvisionReportToExcel(
        GetPayrollJVCustom016ProvisionDiffReportInput input);
}
