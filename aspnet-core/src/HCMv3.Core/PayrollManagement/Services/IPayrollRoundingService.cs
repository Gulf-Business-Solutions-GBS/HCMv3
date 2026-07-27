using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Faithful port of legacy <c>HCM10.PayrollEngine/Program.SetRoundoff</c> (legacy <c>Program.cs</c> lines
/// 3002-3101) and <c>Program.GetDecimalRounding</c> (lines 3102-3121).
///
/// <para><b>Do not "improve" any digit count here.</b> The legacy digit counts are financial controls, not
/// formatting preferences; the exact numbers are documented on each member.</para>
///
/// <para><b>When it runs.</b> Legacy explicitly MOVED this call out of the end of the per-period loop and into
/// the FIRST statement of the 9000 Net Pay wage-type branch (legacy
/// <c>EmployeeCalculatedPayService.cs</c> line 1062; the old call sites at <c>Program.cs</c> lines 1173 and
/// 1376 are commented out with the note "Register amount figures are rounded before NetPay calculation, So we
/// can avoid difference like 0.001 ... faced issue in Mashaan (08), November-2020 offcycle case of Employee
/// code E211"). Wave P4's 9000 Net Pay calculator MUST call <see cref="ApplyRoundingAsync"/> before summing,
/// or that bug returns.</para>
/// </summary>
public interface IPayrollRoundingService : IDomainService
{
    /// <summary>
    /// Rounds Rate/Number/Amount of every line on the register, and of every day-detail row hanging off those
    /// lines, choosing between legacy's two modes:
    ///
    /// <list type="bullet">
    /// <item><description><b>Old mode</b> -- used only when <c>ManagePayrollSettings.Decimal6RoundingFrom</c>
    /// is set (non-default) AND the register's <c>ForPeriod</c> is strictly BEFORE it (legacy line 3010).
    /// Rounds to the employee's currency <c>DecimalCount</c> (see
    /// <see cref="GetCurrencyDecimalRoundingAsync"/>), EXCEPT for the ten annual-leave/ticket accrual wage
    /// codes 4021, 5021, 4010, 5010, 4030, 5030, 4022, 5022, 4031, 5031 (legacy line 3025), which are always
    /// rounded to 6 -- because rounding an accrual rate to 3 decimals loses material precision.</description></item>
    /// <item><description><b>Current mode</b> -- everything else. Rounds to
    /// <c>PayrollEngineSettings.DecimalInSalaryRegister</c> (default 6; legacy applies its zero guard at load
    /// time, see that property) with no wage-code exceptions (legacy lines 3074-3091).</description></item>
    /// </list>
    ///
    /// <para><b>Legacy swallows all exceptions</b> from this method ("//exception while rounding .. skip",
    /// legacy lines 3097-3100) so that a rounding failure never fails a payroll run. That behaviour is
    /// preserved -- see the implementation's remarks.</para>
    /// </summary>
    Task ApplyRoundingAsync(PayrollRegisterRef register, PayrollEngineSettings settings);

    /// <summary>
    /// Legacy <c>Program.GetDecimalRounding(emp)</c> (lines 3102-3121): the employee's payroll-area currency
    /// <c>DecimalCount</c>, defaulting to <b>3</b> when it cannot be resolved (legacy's local initialiser at
    /// line 3104; legacy also swallows any exception and returns that 3).
    /// </summary>
    Task<int> GetCurrencyDecimalRoundingAsync(int employeeId);

    /// <summary>
    /// The digit count legacy uses for the 9000 Net Pay amount (legacy
    /// <c>EmployeeCalculatedPayService.cs</c> line 1076):
    /// <c>(!Program.isNetPayRound) ? Program.GetDecimalRounding(emp) : 2</c> -- i.e. the
    /// <c>ManagePayrollSettings.RoundNetPay</c> flag forces <b>2</b> decimals, otherwise the currency's own
    /// decimal count is used. Note the inverted-looking sense is legacy's, and is deliberate.
    /// </summary>
    Task<int> GetNetPayRoundingDigitsAsync(int employeeId, PayrollEngineSettings settings);
}
