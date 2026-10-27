using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Separation analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeSeparation.ascx</c>).
/// </summary>
public interface IEmployeeSeparationReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSeparationReportRowDto>> GetEmployeeSeparationReport(GetEmployeeSeparationReportInput input);

    Task<FileDto> GetEmployeeSeparationReportToExcel(GetEmployeeSeparationReportInput input);
}
