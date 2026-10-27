using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Dto;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.PersonnelAdministration;

/// <inheritdoc cref="ILeavePlannerAppService"/>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_LeavePlanner)]
public class LeavePlannerAppService : HCMv3AppServiceBase, ILeavePlannerAppService
{
    private readonly IDashboardScopeService _dashboardScopeService;
    private readonly LeavePlannerReader _reader;

    public LeavePlannerAppService(IDashboardScopeService dashboardScopeService, LeavePlannerReader reader)
    {
        _dashboardScopeService = dashboardScopeService;
        _reader = reader;
    }

    [HttpPost]
    public async Task<LeavePlannerDto> GetLeavePlanner(GetLeavePlannerInput input)
    {
        var scope = await _dashboardScopeService.GetScopeAsync();

        // Null = everybody. A restricted scope that resolves to nobody stays an empty list, so the planner shows
        // nothing rather than everything.
        var ids = scope.IsEveryone ? null : scope.EmployeeIds;

        var planner = _reader.Read(input, ids, Clock.Now.Date, L("LeaveDashboard_Unassigned"));

        planner.Scope ??= new DashboardScopeDto();
        planner.Scope.Reason = scope.Reason;
        planner.Scope.OrgUnitNames = scope.OrgUnitNames.ToList();
        planner.Scope.IncludesOwnTeam = scope.IncludesOwnTeam;

        return planner;
    }
}
