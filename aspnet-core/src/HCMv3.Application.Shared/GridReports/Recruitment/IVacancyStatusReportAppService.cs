using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Recruitment.Dto;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Vacancy Status analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptVacancyStatus.ascx</c>).
/// </summary>
public interface IVacancyStatusReportAppService : IApplicationService
{
    Task<PagedResultDto<VacancyStatusReportRowDto>> GetVacancyStatusReport(GetVacancyStatusReportInput input);

    Task<FileDto> GetVacancyStatusReportToExcel(GetVacancyStatusReportInput input);
}
