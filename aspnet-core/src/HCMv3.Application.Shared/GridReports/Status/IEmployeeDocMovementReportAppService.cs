using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Document Movement analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeDocMovement.ascx</c>).
/// </summary>
public interface IEmployeeDocMovementReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocMovementReportRowDto>> GetEmployeeDocMovementReport(GetEmployeeDocMovementReportInput input);

    Task<FileDto> GetEmployeeDocMovementReportToExcel(GetEmployeeDocMovementReportInput input);
}
