using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Probation analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeProbation.ascx</c>).
/// </summary>
public interface IEmployeeProbationReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeProbationReportRowDto>> GetEmployeeProbationReport(GetEmployeeProbationReportInput input);

    Task<FileDto> GetEmployeeProbationReportToExcel(GetEmployeeProbationReportInput input);
}
