using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log (Custom 033) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom033.ascx</c>.
///
/// This is a SEPARATE customer implementation, not a parameterisation of the base
/// <see cref="ILateEarlyLogReportAppService"/>: the two disagree on the column set AND on how a row is
/// computed. See the implementation's remarks for the exact deltas against the base control.
///
/// Same two-operation shape as every other report in this family: a paged read and a full export.
/// </summary>
public interface ILateEarlyLogCustom033ReportAppService : IApplicationService
{
    Task<PagedResultDto<LateEarlyLogCustom033ReportRowDto>> GetLateEarlyLogCustom033Report(
        GetLateEarlyLogCustom033ReportInput input);

    Task<FileDto> GetLateEarlyLogCustom033ReportToExcel(GetLateEarlyLogCustom033ReportInput input);
}
