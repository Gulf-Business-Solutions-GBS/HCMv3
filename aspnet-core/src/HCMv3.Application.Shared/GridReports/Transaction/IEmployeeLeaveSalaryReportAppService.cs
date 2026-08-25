using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Leave Salary analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeLeaveSalary.ascx</c>).
/// </summary>
public interface IEmployeeLeaveSalaryReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLeaveSalaryReportRowDto>> GetEmployeeLeaveSalaryReport(GetEmployeeLeaveSalaryReportInput input);

    Task<FileDto> GetEmployeeLeaveSalaryReportToExcel(GetEmployeeLeaveSalaryReportInput input);
}
