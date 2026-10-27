using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Bank analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeBank.ascx</c>).
/// Same two-operation shape as the reference <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeBankReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeBankReportRowDto>> GetEmployeeBankReport(GetEmployeeBankReportInput input);

    Task<FileDto> GetEmployeeBankReportToExcel(GetEmployeeBankReportInput input);
}
