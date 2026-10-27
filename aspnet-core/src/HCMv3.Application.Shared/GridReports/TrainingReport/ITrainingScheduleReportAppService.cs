using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TrainingReport.Dto;

namespace HCMv3.GridReports.TrainingReport;

/// <summary>
/// Training Schedule analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptTrainingSchedule.ascx</c>).
/// </summary>
public interface ITrainingScheduleReportAppService : IApplicationService
{
    Task<PagedResultDto<TrainingScheduleReportRowDto>> GetTrainingScheduleReport(GetTrainingScheduleReportInput input);

    Task<FileDto> GetTrainingScheduleReportToExcel(GetTrainingScheduleReportInput input);
}
