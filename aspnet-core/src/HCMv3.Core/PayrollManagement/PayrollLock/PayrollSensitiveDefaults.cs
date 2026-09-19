using System;
using System.Collections.Generic;
using HCMv3.BenefitsManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TravelManagement;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// The record types every company starts with as payroll-sensitive: while an employee's payroll area is locked,
/// these cannot be created, changed or deleted for that employee. See <see cref="IPayrollLockGuard"/>.
///
/// <para><b>Where the list comes from.</b> Legacy kept it in its <c>SysObject</c> table, as configuration. The
/// three customer databases on hand (RCF, ASF, CCH) agree on these 31 classes; this is that list, minus
/// <c>EmployeeLoanPayment</c>, which was never ported. A company can still change it on the System Objects
/// screen -- this is only what a company starts with.</para>
///
/// <para>Written as types rather than strings so a renamed or moved entity breaks the build here instead of
/// silently dropping out of the lock.</para>
/// </summary>
public static class PayrollSensitiveDefaults
{
    public static readonly IReadOnlyList<Type> Types = new[]
    {
        typeof(EmployeeInsurance),
        typeof(EmployeeAbsAdjustment),
        typeof(EmployeeAbsence),
        typeof(EmployeeAction),
        typeof(EmployeeAssetInvoice),
        typeof(EmployeeAttendance),
        typeof(EmployeeBasicPay),
        typeof(EmployeeContract),
        typeof(EmployeeDisciplinary),
        typeof(EmployeeFnFPayment),
        typeof(EmployeeGratuity),
        typeof(EmployeeHealthReimburse),
        typeof(EmployeeLoan),
        typeof(EmployeeOnHold),
        typeof(EmployeeOrgAssignment),
        typeof(EmployeeOvertime),
        typeof(EmployeePayment),
        typeof(EmployeeRecurringPayment),
        typeof(EmployeeRejoin),
        typeof(EmployeeRemuneration),
        typeof(EmployeeSeparation),
        typeof(EmployeeSocialSecurityClaim),
        typeof(EmployeeTicket),
        typeof(EmployeeTicketRoute),
        typeof(EmployeeTktAdjustment),
        typeof(EmployeeDutyPeriod),
        typeof(EmployeeSubstituteDuty),
        typeof(EmployeeTimePolicy),
        typeof(Travel),
        typeof(TravelDetail),
    };
}
