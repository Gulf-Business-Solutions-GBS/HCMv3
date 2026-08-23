using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Info analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeInfo.ascx</c>).
/// Both operations are <c>[HttpPost]</c> in the implementation because they take a custom input DTO.
/// </summary>
public interface IEmployeeInfoReportAppService : IApplicationService
{
    Task<EmployeeInfoReportOutput> GetEmployeeInfoReport(GetEmployeeInfoReportInput input);

    Task<FileDto> GetEmployeeInfoReportToExcel(GetEmployeeInfoReportInput input);
}
