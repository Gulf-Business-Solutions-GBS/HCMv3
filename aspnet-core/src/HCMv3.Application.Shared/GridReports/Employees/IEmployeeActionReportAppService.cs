using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Action analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeAction.ascx</c>).
/// Same two-operation shape as the reference <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeActionReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeActionReportRowDto>> GetEmployeeActionReport(GetEmployeeActionReportInput input);

    Task<FileDto> GetEmployeeActionReportToExcel(GetEmployeeActionReportInput input);
}
