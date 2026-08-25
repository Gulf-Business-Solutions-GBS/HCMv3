using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.GovtRelations.Dto;

namespace HCMv3.GridReports.GovtRelations;

/// <summary>
/// Sponsor analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/GovtRelations/uscRptSponsor.ascx</c>).
/// </summary>
public interface ISponsorReportAppService : IApplicationService
{
    Task<PagedResultDto<SponsorReportRowDto>> GetSponsorReport(GetSponsorReportInput input);

    Task<FileDto> GetSponsorReportToExcel(GetSponsorReportInput input);
}
