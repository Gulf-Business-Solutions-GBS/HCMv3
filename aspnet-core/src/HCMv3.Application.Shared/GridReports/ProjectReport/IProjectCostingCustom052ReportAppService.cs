using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.ProjectReport.Dto;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Costing (Custom052) analysis grid report -- per-customer variant of
/// <see cref="IProjectCostingReportAppService"/> (port of legacy <c>uscRptProjectCosting_Custom052.ascx</c>).
/// </summary>
public interface IProjectCostingCustom052ReportAppService : IApplicationService
{
    Task<PagedResultDto<ProjectCostingCustom052ReportRowDto>> GetProjectCostingCustom052Report(
        GetProjectCostingCustom052ReportInput input);

    Task<FileDto> GetProjectCostingCustom052ReportToExcel(GetProjectCostingCustom052ReportInput input);
}
