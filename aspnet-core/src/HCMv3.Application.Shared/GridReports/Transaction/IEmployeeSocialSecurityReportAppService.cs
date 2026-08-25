using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Social Security analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeSocialSecurity.ascx</c>).
/// </summary>
public interface IEmployeeSocialSecurityReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSocialSecurityReportRowDto>> GetEmployeeSocialSecurityReport(
        GetEmployeeSocialSecurityReportInput input);

    Task<FileDto> GetEmployeeSocialSecurityReportToExcel(GetEmployeeSocialSecurityReportInput input);
}
