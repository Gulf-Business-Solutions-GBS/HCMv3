using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.SurveyReport.Dto;

namespace HCMv3.GridReports.SurveyReport;

/// <summary>
/// Employee Survey analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/SurveyReport/uscRptEmployeeSurvey.ascx</c>).
/// </summary>
public interface IEmployeeSurveyReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSurveyReportRowDto>> GetEmployeeSurveyReport(GetEmployeeSurveyReportInput input);

    Task<FileDto> GetEmployeeSurveyReportToExcel(GetEmployeeSurveyReportInput input);
}
