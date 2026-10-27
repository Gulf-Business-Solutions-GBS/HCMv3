using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// When an employee's probation actually ends. Legacy <c>Employee.GetProbationEndDate</c> had two modes:
/// plain calendar days, or WORKING days -- where a day the employee was not available does not count, so
/// unpaid leave, absence, holidays and week-offs push the end of probation out.
///
/// <para>
/// The working-days mode was deliberately left out of the original port because it needs the time engine's
/// data; that exists now. The calendar-days mode is unchanged: joining date plus the probation days.
/// </para>
/// </summary>
public interface IProbationEndDateService : IDomainService
{
    /// <summary>Works out the date without saving anything.</summary>
    Task<DateTime> CalculateAsync(int employeeId);

    /// <summary>
    /// Recomputes and stores the employee's probation end date after a change to their unpaid leave. Does
    /// nothing in calendar-days mode, where leave cannot move it.
    /// </summary>
    Task RecalculateAfterLeaveChangeAsync(int employeeId, int absenceTypeId);
}
