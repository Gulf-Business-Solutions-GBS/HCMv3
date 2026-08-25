using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Administration.Dto;

namespace HCMv3.GridReports.Administration;

/// <summary>
/// Execution Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Administration/uscRptExecutionLogReport.ascx</c>).
/// </summary>
public interface IExecutionLogReportAppService : IApplicationService
{
    Task<PagedResultDto<ExecutionLogReportRowDto>> GetExecutionLogReport(GetExecutionLogReportInput input);

    Task<FileDto> GetExecutionLogReportToExcel(GetExecutionLogReportInput input);
}
