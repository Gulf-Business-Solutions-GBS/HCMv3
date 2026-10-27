using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Employee Duty Period analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmployeeDutyPeriod.ascx</c>).
/// Same two-operation shape as the reference <c>IEmployeeContractReportAppService</c>.
/// </summary>
public interface IEmployeeDutyPeriodReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDutyPeriodReportRowDto>> GetEmployeeDutyPeriodReport(
        GetEmployeeDutyPeriodReportInput input);

    Task<FileDto> GetEmployeeDutyPeriodReportToExcel(GetEmployeeDutyPeriodReportInput input);
}
