using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

/// <summary>
/// Read-only aggregation service backing the ported legacy roster scheduling screens
/// <c>HCM10.Web/UserControls/TimeManagement/uscRosterDutySchedule.ascx(.cs)</c> and
/// <c>uscRosterSchedule.ascx(.cs)</c>.
///
/// SCOPE NOTE: both legacy screens are viewers, not editors. Neither persists anything (see
/// <c>RosterScheduleViewerAppService</c> remarks for the evidence). All CRUD over the underlying roster
/// entities is already covered by the existing per-entity app services
/// (<see cref="IDutyPeriodsAppService"/>, <see cref="IEmployeeDutyPeriodsAppService"/>,
/// <see cref="IRosterGroupsAppService"/>, <see cref="IRosterGroupDetailsAppService"/>,
/// <see cref="IRosterGroupSchedulesAppService"/>, <see cref="IRosterDutySchedulesAppService"/>,
/// <see cref="IRosterDailyDutiesAppService"/>), so this service adds only the two read projections the
/// existing CRUD services do not provide.
/// </summary>
public interface IRosterScheduleViewerAppService : IApplicationService
{
    /// <summary>
    /// Employees assigned to a duty period, directly (<c>EmployeeDutyPeriod</c>) or via a roster group
    /// (<c>RosterGroupSchedule</c> -&gt; <c>RosterGroupDetail</c>).
    /// Legacy: <c>uscRosterDutySchedule.ascx.cs</c> -&gt; <c>FillEmployeeGrid</c>.
    /// </summary>
    Task<PagedResultDto<RosterDutyScheduleEmployeeDto>> GetRosterDutyScheduleEmployees(GetRosterDutyScheduleEmployeesInput input);

    /// <summary>
    /// Per-employee, per-day resolved <c>Duty</c> across a date range.
    /// Legacy: <c>uscRosterDutySchedule.ascx.cs</c> -&gt; <c>BindSchedular</c>.
    /// </summary>
    Task<GetRosterDutyScheduleOutput> GetRosterDutySchedule(GetRosterDutyScheduleInput input);
}
