using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog.ascx</c> -- the BASE control
/// only; its seven <c>_Custom0XX</c> variants and <c>_Old</c> are out of scope).
/// Same two-operation shape as the reference <c>IEmployeeContractReportAppService</c>.
/// </summary>
public interface ILateEarlyLogReportAppService : IApplicationService
{
    Task<PagedResultDto<LateEarlyLogReportRowDto>> GetLateEarlyLogReport(GetLateEarlyLogReportInput input);

    Task<FileDto> GetLateEarlyLogReportToExcel(GetLateEarlyLogReportInput input);
}
