using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Recruitment.Dto;

namespace HCMv3.GridReports.Recruitment;

/// <summary>
/// Applicant Search analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Recruitment/uscRptApplicantSearch.ascx</c>).
/// </summary>
public interface IApplicantSearchReportAppService : IApplicationService
{
    Task<PagedResultDto<ApplicantSearchReportRowDto>> GetApplicantSearchReport(GetApplicantSearchReportInput input);

    Task<FileDto> GetApplicantSearchReportToExcel(GetApplicantSearchReportInput input);
}
