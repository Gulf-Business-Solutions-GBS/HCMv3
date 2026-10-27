using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee On-Hold analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeOnHold.ascx</c>).
/// </summary>
public interface IEmployeeOnHoldReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeOnHoldReportRowDto>> GetEmployeeOnHoldReport(GetEmployeeOnHoldReportInput input);

    Task<FileDto> GetEmployeeOnHoldReportToExcel(GetEmployeeOnHoldReportInput input);
}
