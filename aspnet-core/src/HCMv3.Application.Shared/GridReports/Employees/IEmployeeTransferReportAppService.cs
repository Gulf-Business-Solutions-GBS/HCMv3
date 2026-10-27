using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Transfer analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeTransfer.ascx</c>).
/// </summary>
public interface IEmployeeTransferReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTransferReportRowDto>> GetEmployeeTransferReport(
        GetEmployeeTransferReportInput input);

    Task<FileDto> GetEmployeeTransferReportToExcel(GetEmployeeTransferReportInput input);
}
