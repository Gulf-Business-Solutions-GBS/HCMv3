using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Custom045 Late/Early Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom045.ascx</c>).
///
/// A SEPARATE customer implementation of the Late/Early Log, not a parameterisation of
/// <see cref="ILateEarlyLogReportAppService"/> -- see the app service remarks for why the two are not
/// folded into a shared base. Same two-operation shape as every other report in the family.
/// </summary>
public interface ILateEarlyLogCustom045ReportAppService : IApplicationService
{
    Task<PagedResultDto<LateEarlyLogCustom045ReportRowDto>> GetLateEarlyLogCustom045Report(
        GetLateEarlyLogCustom045ReportInput input);

    Task<FileDto> GetLateEarlyLogCustom045ReportToExcel(GetLateEarlyLogCustom045ReportInput input);
}
