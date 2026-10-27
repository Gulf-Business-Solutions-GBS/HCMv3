using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.ProjectReport.Dto;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Costing analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectCosting.ascx</c>).
/// ATYPICAL: rows carry a dynamic wage-type pivot -- see <see cref="Dto.ProjectCostingReportRowDto"/>.
/// </summary>
public interface IProjectCostingReportAppService : IApplicationService
{
    Task<PagedResultDto<ProjectCostingReportRowDto>> GetProjectCostingReport(GetProjectCostingReportInput input);

    Task<FileDto> GetProjectCostingReportToExcel(GetProjectCostingReportInput input);
}
