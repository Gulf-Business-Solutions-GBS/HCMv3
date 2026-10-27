using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports;

/// <summary>
/// Column captions for the OrgLevel1..OrgLevel7 columns shared by the analysis grid reports. Port of
/// legacy <c>OrgService.GetOrgUnitTypeColName</c>, which every legacy grid report called once per level.
/// </summary>
public interface IGridReportOrgLevelsAppService : IApplicationService
{
    /// <summary>
    /// One entry per level (1..7) the tenant has defined as an Organization Unit Type, in level order.
    /// Levels with no Organization Unit Type are left out, so the client can hide those columns.
    /// </summary>
    Task<ListResultDto<OrgLevelCaptionDto>> GetOrgLevelCaptions();
}
