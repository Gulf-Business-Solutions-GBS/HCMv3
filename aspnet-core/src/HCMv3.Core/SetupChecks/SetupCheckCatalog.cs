using System;
using System.Collections.Generic;

namespace HCMv3.SetupChecks;

/// <summary>
/// Every tenant setup check, in the order the screen lists them. Kept as an explicit list rather than discovered,
/// so a reviewer sees exactly what a release adds; a guard test fails if an <see cref="ISetupCheck"/> is missing here.
/// </summary>
public static class SetupCheckCatalog
{
    public static readonly IReadOnlyList<Type> All = new List<Type>
    {
        // ---- 3.0.0: payroll
        typeof(PayrollAreaExistsCheck),
        typeof(PayrollControlRecordCheck),
        typeof(PayrollCalendarCurrentMonthCheck),
        typeof(SystemSettingsRowsCheck),
        typeof(WageTypeLeaveLinksCheck),

        // ---- 3.0.0: employee profile
        typeof(EmployeeContractCheck),
        typeof(EmployeeOrgAssignmentCheck),
        typeof(EmployeePayrollAreaCheck),
        typeof(EmployeeGroupAndSubGroupCheck),
        typeof(EmployeeSalaryRecordCheck),
        typeof(EmployeePayrollStatusCheck),
        typeof(EmployeeDutyPeriodCheck),
        typeof(EmployeeTimePolicyCheck),
        typeof(EmployeePaymentDetailsCheck),
        typeof(EmployeeBankAccountCheck),
    };
}
