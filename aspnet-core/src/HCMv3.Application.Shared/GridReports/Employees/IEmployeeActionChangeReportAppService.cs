using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Action Change analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeActionChange.ascx</c>).
/// Same two-operation shape as the reference <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeActionChangeReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeActionChangeReportRowDto>> GetEmployeeActionChangeReport(
        GetEmployeeActionChangeReportInput input);

    Task<FileDto> GetEmployeeActionChangeReportToExcel(GetEmployeeActionChangeReportInput input);
}
