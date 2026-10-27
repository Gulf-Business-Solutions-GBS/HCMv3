using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Schedule analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeSchedule.ascx</c>).
/// Same two-operation shape as the reference <c>IEmployeeContractReportAppService</c>.
/// </summary>
public interface ITimeScheduleReportAppService : IApplicationService
{
    Task<PagedResultDto<TimeScheduleReportRowDto>> GetTimeScheduleReport(GetTimeScheduleReportInput input);

    Task<FileDto> GetTimeScheduleReportToExcel(GetTimeScheduleReportInput input);
}
