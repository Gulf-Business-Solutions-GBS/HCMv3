using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee CTC analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeCTC.ascx</c>).
/// Both operations are <c>[HttpPost]</c> in the implementation because they take a custom input DTO.
/// </summary>
public interface IEmployeeCTCReportAppService : IApplicationService
{
    Task<EmployeeCTCReportOutput> GetEmployeeCTCReport(GetEmployeeCTCReportInput input);

    Task<FileDto> GetEmployeeCTCReportToExcel(GetEmployeeCTCReportInput input);
}
