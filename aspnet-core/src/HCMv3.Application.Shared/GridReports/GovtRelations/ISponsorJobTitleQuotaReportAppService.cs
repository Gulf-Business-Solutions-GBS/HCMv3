using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.GovtRelations.Dto;

namespace HCMv3.GridReports.GovtRelations;

/// <summary>
/// Sponsor Job Title Quota analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/GovtRelations/uscRptSponsorJobTitleQuota.ascx</c>).
/// </summary>
public interface ISponsorJobTitleQuotaReportAppService : IApplicationService
{
    Task<PagedResultDto<SponsorJobTitleQuotaReportRowDto>> GetSponsorJobTitleQuotaReport(GetSponsorJobTitleQuotaReportInput input);

    Task<FileDto> GetSponsorJobTitleQuotaReportToExcel(GetSponsorJobTitleQuotaReportInput input);
}
