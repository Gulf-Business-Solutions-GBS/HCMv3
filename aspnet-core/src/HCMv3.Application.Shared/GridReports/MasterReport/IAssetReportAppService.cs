using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.MasterReport.Dto;

namespace HCMv3.GridReports.MasterReport;

/// <summary>
/// Asset analysis grid report (port of legacy <c>MasterReport/uscRptAsset.ascx</c>).
/// </summary>
public interface IAssetReportAppService : IApplicationService
{
    Task<PagedResultDto<AssetReportRowDto>> GetAssetReport(GetAssetReportInput input);

    Task<FileDto> GetAssetReportToExcel(GetAssetReportInput input);
}
