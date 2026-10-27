using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Basic Pay analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeBasicPay.ascx</c>).
///
/// Still the standard two operations. The legacy control had a SECOND level (<c>Table2()</c>, the wage
/// lines of the selected pay record) reached through the DevExpress drill-down; no third endpoint is
/// added for it because those wage lines are already carried on every row in
/// <c>EmployeeBasicPayReportRowDto.WageAmounts</c>.
/// </summary>
public interface IEmployeeBasicPayReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBasicPayReportRowDto>> GetEmployeeBasicPayReport(
        GetEmployeeBasicPayReportInput input);

    Task<FileDto> GetEmployeeBasicPayReportToExcel(GetEmployeeBasicPayReportInput input);
}
