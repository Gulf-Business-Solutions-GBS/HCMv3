using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The Leave Planner (legacy <c>uscLeavePlanner</c> under Reports > Analysis > Transaction): planned and actual
/// leave by organization unit and month.
///
/// <para>
/// Whose leave is shown follows the logged-in user, as every HCMv3 dashboard does (<c>IDashboardScopeService</c>):
/// the tenant Admin role sees everybody; anybody else sees the org units their roles are given in row-level
/// security plus their own team; nothing assigned shows nobody. Legacy applied row-level security here too
/// (<c>uscLeavePlanner.ApplyRowLevelSecurity</c>).
/// </para>
/// </summary>
public interface ILeavePlannerAppService : IApplicationService
{
    Task<LeavePlannerDto> GetLeavePlanner(GetLeavePlannerInput input);
}
