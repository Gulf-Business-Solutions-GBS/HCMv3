using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Deduction analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeDeduction.ascx</c>).
/// Both operations are <c>[HttpPost]</c> in the implementation because they take a custom input DTO.
/// </summary>
public interface IEmployeeDeductionReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDeductionReportRowDto>> GetEmployeeDeductionReport(
        GetEmployeeDeductionReportInput input);

    Task<FileDto> GetEmployeeDeductionReportToExcel(GetEmployeeDeductionReportInput input);
}
