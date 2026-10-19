using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The "Expected Overtime Amount" of an <see cref="EmployeeOvertime"/> row: what the approved hours are
/// worth, worked out the way legacy did it on screen. Ported from the legacy XAF non-persistent
/// <c>EmployeeOvertime.ExpectedOvertimeAmount</c> getter and its private <c>GetExpectedOvertimeAmount</c>
/// (<c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeOvertime.cs</c> lines 84-98 and
/// 197-303).
///
/// <para>Read-only: nothing is stored. It is a preview figure and is NOT what payroll pays -- payroll uses
/// <c>EmployeeOvertimePayCalculationService</c>, whose "Actual" rate reads the salary-register lines rather
/// than the basic-pay record.</para>
/// </summary>
public interface IExpectedOvertimeAmountService : IDomainService
{
    /// <summary>
    /// Legacy getter (line 92) gate plus <c>GetExpectedOvertimeAmount</c>. Returns 0 when the employee or
    /// overtime type is missing, the effective date is unset, or the approved hours are 0.
    /// </summary>
    Task<double> GetExpectedOvertimeAmountAsync(int employeeId, int overtimeTypeId, DateTime effectiveDate, double approvedOvertimeHours);

    /// <summary>Same as the single-row overload for each row, reading payroll settings once.</summary>
    Task<Dictionary<int, double>> GetExpectedOvertimeAmountsAsync(IEnumerable<EmployeeOvertime> overtimes);
}
