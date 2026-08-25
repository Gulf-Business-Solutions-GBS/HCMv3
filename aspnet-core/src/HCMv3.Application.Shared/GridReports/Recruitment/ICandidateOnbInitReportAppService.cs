using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Recruitment.Dto;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Candidate Onboarding Init analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptCandidateOnbInit.ascx</c>).
///
/// Legacy's data contract (<c>Table1()</c>) is byte-for-byte identical to
/// <c>uscRptVacancyStatus.ascx.cs</c> -- see <see cref="Dto.VacancyStatusReportRowDto"/> remarks. This is a
/// separate app service (and its own DTO input class) only because it is a separate legacy screen with its
/// own permission and route, matching the task's screen-to-service mapping; it shares
/// <see cref="Dto.VacancyStatusReportRowDto"/> and delegates its query to <c>VacancyStatusReportQueryHelper</c>
/// so the two cannot drift.
/// </summary>
public interface ICandidateOnbInitReportAppService : IApplicationService
{
    Task<PagedResultDto<VacancyStatusReportRowDto>> GetCandidateOnbInitReport(GetVacancyStatusReportInput input);

    Task<FileDto> GetCandidateOnbInitReportToExcel(GetVacancyStatusReportInput input);
}
