using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Organization.Dto;

namespace HCMv3.GridReports.Organization;

/// <summary>
/// Organization Document analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Organization/uscRptOrganizationDocument.ascx</c>).
/// </summary>
public interface IOrganizationDocumentReportAppService : IApplicationService
{
    Task<PagedResultDto<OrganizationDocumentReportRowDto>> GetOrganizationDocumentReport(GetOrganizationDocumentReportInput input);

    Task<FileDto> GetOrganizationDocumentReportToExcel(GetOrganizationDocumentReportInput input);
}
