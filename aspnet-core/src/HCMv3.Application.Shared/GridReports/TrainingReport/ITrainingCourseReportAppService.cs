using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TrainingReport.Dto;

namespace HCMv3.GridReports.TrainingReport;

/// <summary>
/// Training Course analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptTrainingCourse.ascx</c>).
/// </summary>
public interface ITrainingCourseReportAppService : IApplicationService
{
    Task<PagedResultDto<TrainingCourseReportRowDto>> GetTrainingCourseReport(GetTrainingCourseReportInput input);

    Task<FileDto> GetTrainingCourseReportToExcel(GetTrainingCourseReportInput input);
}
