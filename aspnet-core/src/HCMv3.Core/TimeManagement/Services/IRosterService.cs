using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.RosterService</c>.
/// </summary>
public interface IRosterService
{
    /// <summary>Ported from legacy <c>RosterService.GetRosterDutyPeriod</c>.</summary>
    Task<DutyPeriod> GetRosterDutyPeriodAsync(int employeeId, DateTime dayDate);

    /// <summary>Ported from legacy <c>RosterService.GetRosterDuty</c>.</summary>
    Task<Duty> GetRosterDutyAsync(int employeeId, DateTime dayDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, bool throwException = true);
}
