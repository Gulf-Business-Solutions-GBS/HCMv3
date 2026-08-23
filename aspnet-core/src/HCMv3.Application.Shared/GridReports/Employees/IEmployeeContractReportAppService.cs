using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Contract analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeContract.ascx</c>).
///
/// This is the REFERENCE SHAPE for the ~140 legacy grid reports: exactly two operations, one paged
/// query and one "export everything that matches the same filters" -- both taking the identical input
/// DTO. Both are <c>[HttpPost]</c> in the implementation because they take a custom input DTO.
/// </summary>
public interface IEmployeeContractReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeContractReportRowDto>> GetEmployeeContractReport(GetEmployeeContractReportInput input);

    Task<FileDto> GetEmployeeContractReportToExcel(GetEmployeeContractReportInput input);
}
