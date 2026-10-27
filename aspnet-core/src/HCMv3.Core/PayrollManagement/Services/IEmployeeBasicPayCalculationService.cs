using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Port of legacy <c>HCM10.PayrollEngine/EmployeeBasicPayService.Main</c> (legacy lines 16-1145).
///
/// <para><b>Why this is NOT an <see cref="IWageTypeCalculator"/> -- read before "fixing" it.</b>
/// <see cref="IWageTypeCalculator"/> keys a rule to ONE compile-time <c>WageCode</c>. Basic pay has no such
/// code. Legacy dispatched to <c>EmployeeBasicPayService</c> via
/// <c>wtBasicPayWageList.Contains(currWageType.Oid)</c> (legacy <c>Program.cs</c> line 2538), and that list is
/// built PER EMPLOYEE from the employee's own salary structure:
/// <c>wtBasicPayWageList = empBasicPayDetailList.Select(r =&gt; r.WageType.Oid).ToList()</c>
/// (legacy <c>Program.cs</c> line 1923, from <c>emp.BasicPay...BasicPayDetail</c> at line 1918). Two employees
/// on the same run legitimately route different wage codes here. A code-keyed -- or even a
/// static-code-LIST-keyed -- registry cannot express that; only a per-context predicate can.</para>
///
/// <para>The literal wage codes that DO appear in the legacy body ("1000", "4007", "4008", "4009", "4011",
/// "4014") are every one of them <b>lookups of OTHER wage types' already-written register lines</b>
/// (day counts and the 1000 Basic Pay reference row), never a branch on <c>currWageType.Code</c>. Verified
/// line by line: legacy 130, 144, 148, 163, 182, 242, 256, 270, 292, 324, 339, 623, 978. The real branching is
/// on <c>EmployeeBasicPay.PayInterval</c>, <c>WageType.BillingMethod</c> and <c>WageType.ProrateMethod</c>.</para>
///
/// <para><b>Consequence for the engine (wave P1b).</b> Category dispatch stays in the engine. The engine calls
/// <see cref="GetBasicPayWageTypeIdsAsync"/> once per employee to reproduce <c>wtBasicPayWageList</c>, and
/// routes matching wage types to <see cref="CalculateAsync"/> instead of to
/// <see cref="IWageTypeCalculatorResolver"/>.</para>
///
/// <para><b>Gate NOT covered by <see cref="IWageTypeCalculatorResolver"/>.</b> Legacy's basic-pay arm at
/// <c>Program.cs</c> line 2538 is reached only after <c>Allow_CalcWageType</c>, and passes
/// <c>onHold_BasicPay</c> INTO the service rather than gating on it (the flag suppresses Number/Amount but
/// still writes Rate -- legacy lines 224-228). That is preserved here, reading
/// <see cref="PayrollCalculationContext.OnHold"/>. There is no legacy <c>IsOnHold</c>/<c>AllowToCalculate</c>
/// call on this path at all; if P1b routes basic pay through the resolver's gates it will be ADDING
/// behaviour.</para>
/// </summary>
public interface IEmployeeBasicPayCalculationService : IDomainService
{
    /// <summary>
    /// Legacy <c>Program.wtBasicPayWageList</c> (legacy <c>Program.cs</c> lines 1918-1932): the ids of the
    /// wage types on the employee's <c>EmployeeBasicPay</c> row covering <paramref name="payDate"/>. Empty
    /// when no basic-pay row covers the date (legacy clears the list -- line 1929).
    /// </summary>
    Task<List<int>> GetBasicPayWageTypeIdsAsync(int employeeId, DateTime payDate);

    /// <summary>
    /// Legacy <c>EmployeeBasicPayService.Main</c>. <paramref name="context"/>'s
    /// <see cref="PayrollCalculationContext.WageType"/> must be one of the ids returned by
    /// <see cref="GetBasicPayWageTypeIdsAsync"/>; anything else is a no-op (legacy's
    /// <c>empBasicPayDetail == null</c> arm, line 25).
    /// </summary>
    Task CalculateAsync(PayrollCalculationContext context);
}
