using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Residency analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeResidency.ascx</c>).
/// </summary>
public interface IEmployeeResidencyReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeResidencyReportRowDto>> GetEmployeeResidencyReport(GetEmployeeResidencyReportInput input);

    Task<FileDto> GetEmployeeResidencyReportToExcel(GetEmployeeResidencyReportInput input);
}
