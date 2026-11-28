using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Keeps an employee's <see cref="EmployeeGratuityQuota"/> (Indemnity Quota) in step with the employee and
/// separation records, as legacy XAF did from <c>Employee.OnSaving</c> and <c>EmployeeSeparation.SaveOtherInfo</c>.
/// The other writers of the quota live elsewhere: the Time Engine creates the row, the Payroll Engine sets
/// Accrued/Used from wage types 4025/4005, Indemnity payments move Used, and Rejoin carries the balance over.
/// </summary>
public interface IEmployeeIndemnityQuotaSyncService : IDomainService
{
    /// <summary>
    /// Legacy <c>Employee.OnSaving</c> "Update Indemnity StartDate": when the joining date changes, the employee's
    /// earliest quota row starts on the new joining date (both EntitlementFrom and DeductionFrom).
    /// </summary>
    Task AlignToJoiningDateAsync(int employeeId, DateTime joiningDate);

    /// <summary>
    /// Legacy <c>EmployeeSeparation.SaveOtherInfo</c> "Advance EndOfService": when the separation reason allows
    /// advance end of service, the employee's quota is accrued up to the last day in service
    /// (<c>AccrualService.GetTotalIndemnityDays</c>). Returns false when nothing changed.
    /// </summary>
    Task<bool> ApplyAdvanceEndOfServiceAsync(int employeeId, int separationReasonId, DateTime lastDayInService);
}
