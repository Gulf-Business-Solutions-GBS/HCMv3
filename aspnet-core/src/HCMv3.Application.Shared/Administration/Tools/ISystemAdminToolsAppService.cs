using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Administration.Tools.Dto;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Ported from legacy <c>HCM10.Web/UserControls/Administration/uscSystemConfigPanel.ascx.cs</c>.
///
/// Like uscPayrollConfigPanel this is a maintenance-utility panel, not a settings screen — nothing here
/// overlaps the 14 ported <c>Manage*Settings</c> singletons.
///
/// TWO OF THE SIX LEGACY PANELS ARE DELIBERATELY NOT PORTED:
/// <list type="bullet">
/// <item><description><b>"Reset Model Navigation Items Nodes"</b> (legacy line 83) rewrote XAF
/// <c>ModelDifferenceAspect.Xml</c> to un-hide navigation items. XAF's application model does not exist in
/// this stack; menu visibility is <c>AppNavigationProvider</c> + the ABP permission tree. Closed out.</description></item>
/// <item><description><b>"Migrate Attachments"</b> (legacy line 309, ~480 lines) was a ONE-TIME data
/// migration moving XPO <c>FileData</c> blobs into <c>FileSysAttachment</c>/disk, plus photo compression
/// and orphan cleanup. It targets legacy storage tables that this port does not have. Closed out; if the
/// migration is still needed it belongs in the data-migration project, not in an API.</description></item>
/// </list>
/// </summary>
public interface ISystemAdminToolsAppService : IApplicationService
{
    /// <summary>
    /// Legacy line 795 ("Recalculate ProbationDays"). Recomputes
    /// <c>Employee.ProbationEndDate</c> for every active employee.
    /// </summary>
    Task<AdminToolResultDto> RecalculateProbationEndDates();

    /// <summary>
    /// Legacy line 844 ("Fetch Time Attendance Missing Data"). Re-flags missing punches in the access
    /// control database so they get re-transferred.
    /// </summary>
    Task<AdminToolResultDto> FetchMissingTimeAttendanceData(FetchMissingTimeAttendanceDataInput input);

    /// <summary>
    /// Legacy line 899 ("Sync Time Attendance Missing API Data"). Re-pulls missing punches from the
    /// time-attendance vendor API.
    /// </summary>
    Task<AdminToolResultDto> SyncMissingTimeAttendanceApiData(FetchMissingTimeAttendanceDataInput input);
}
