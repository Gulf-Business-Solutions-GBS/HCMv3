using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Custom053 Late/Early Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom053.ascx</c>).
///
/// A SEPARATE customer implementation of the Late/Early Log, not a parameterisation of
/// <see cref="ILateEarlyLogReportAppService"/> -- see the app service remarks for why the two are not
/// folded into a shared base. Same two-operation shape as every other report in the family.
/// </summary>
public interface ILateEarlyLogCustom053ReportAppService : IApplicationService
{
    Task<PagedResultDto<LateEarlyLogCustom053ReportRowDto>> GetLateEarlyLogCustom053Report(
        GetLateEarlyLogCustom053ReportInput input);

    Task<FileDto> GetLateEarlyLogCustom053ReportToExcel(GetLateEarlyLogCustom053ReportInput input);
}
