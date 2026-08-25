using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.SurveyReport.Dto;

namespace HCMv3.GridReports.SurveyReport;

/// <summary>
/// Survey Status analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/SurveyReport/uscRptSurveyStatus.ascx</c>).
/// </summary>
public interface ISurveyStatusReportAppService : IApplicationService
{
    Task<PagedResultDto<SurveyStatusReportRowDto>> GetSurveyStatusReport(GetSurveyStatusReportInput input);

    Task<FileDto> GetSurveyStatusReportToExcel(GetSurveyStatusReportInput input);
}
