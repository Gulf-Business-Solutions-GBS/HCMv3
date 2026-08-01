using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollArrearCalculationService"/>
/// <remarks>
/// <b>WAVE P9.</b> Ported from legacy <c>HCM10.PayrollEngine/PayrollArrearService.cs</c>: <c>Main</c>
/// (legacy line 13) and its <c>GetArrear</c> (legacy line 84). <c>Main_Simu</c>/<c>GetArrear_Simu</c>
/// (legacy lines 133-238) are deliberately NOT ported -- wave P7 collapses the simulation clone by flipping
/// <see cref="PayrollRegisterRef.Target"/> on <see cref="IPayrollRegisterStore"/>.
///
/// <para><b>This is the FIRST arm of the engine's dispatch chain</b> (legacy <c>Program.cs</c> line 2532,
/// <c>currWageType.IncludeInArrearPay</c>) and it is evaluated for EVERY active non-TimeType wage type of
/// EVERY employee, whether or not that employee has any arrear. That is why leaving it unported aborted every
/// employee at wage code <c>7000 Basic Salary Arrear</c> (PayrollOrder 20020, immediately after
/// <c>1000 Basic Salary</c>) -- the failure was structural, not data-driven.</para>
///
/// <para><b>NOT the same function as <c>Program.GetArrear</c></b> (legacy <c>Program.cs</c> line 5302, the
/// one <see cref="ITimeTypeCumulationService"/> ports for the 4xxx/5xxx time-type twins). The two differ in
/// three load-bearing ways, so do NOT unify them:</para>
/// <list type="bullet">
/// <item><description>Source wage type: this one uses <see cref="WageType.ArrearSourceWageType"/> off the
/// CURRENT wage type; <c>Program.GetArrear</c> takes a hard-coded wage CODE argument.</description></item>
/// <item><description>Prior-register predicate: this one uses <c>r.InPeriod != payInDate</c> (legacy
/// <c>PayrollArrearService.cs</c> line 100); <c>Program.GetArrear</c> uses <c>r.InPeriod &lt; payInDate</c>
/// (legacy <c>Program.cs</c> line 5325, explicitly changed there in 2023 to ignore future-dated arrear
/// registers -- and explicitly NOT changed here).</description></item>
/// <item><description>Run-type gate: this one only looks for a prior register at all when the run is
/// <see cref="PayrollRunType.Regular"/> or an End-Of-Service off-cycle (legacy line 96);
/// <c>Program.GetArrear</c> has no such gate.</description></item>
/// </list>
///
/// <para><b>Preserved legacy quirks -- all deliberate, none "fixed":</b></para>
/// <list type="number">
/// <item><description><b>The <c>IsNotReturnFromLeave</c> arm is EMPTY and it swallows the whole
/// calculation.</b> Legacy line 22 is <c>if (IsNotReturnFromLeave &amp;&amp; BillingType != Provision) { /*
/// body entirely commented out */ } else if (...)</c>. Because it is an <c>else if</c>, a
/// not-returned-from-leave employee gets NO arrear line at all for any non-Provision wage type -- not a zeroed
/// one. Reproduced exactly (an early <c>return</c>).</description></item>
/// <item><description><b>Zero does NOT delete the row.</b> Legacy line 73 sets <c>Amount = 0</c> on an
/// existing detail instead of deleting it (and creates nothing when none exists), so
/// <see cref="IPayrollRegisterStore.SaveLineAsync"/> is called with
/// <c>deleteWhenAmountIsZero: false</c>.</description></item>
/// <item><description><b><c>Rate</c> and <c>Number</c> are left untouched.</b> Legacy lines 57-58 comment out
/// the <c>Rate = 0; Number = 0;</c> assignments, so an existing line keeps whatever the time-type cumulation
/// pre-pass or another rule put there. Only <c>Amount</c> is written.</description></item>
/// <item><description><b>A missing source line contributes 0, it does not <c>continue</c>.</b> Legacy line
/// 108 (with its comment) removed the <c>continue</c> on purpose so that a wage type paid last month but
/// deleted this month still produces its negative reversal.</description></item>
/// <item><description><b><c>payDate == payInDate</c> is tested TWICE</b> -- once in <c>Main</c> (legacy line
/// 32) and again at the top of <c>GetArrear</c> (legacy line 91). Both are kept; the second is
/// redundant.</description></item>
/// </list>
///
/// <para><b>No EF Core.</b> <c>HCMv3.Core</c> has no EntityFrameworkCore reference, so all reads are plain
/// synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>.</para>
/// </remarks>
public class PayrollArrearCalculationService : HCMv3DomainServiceBase, IPayrollArrearCalculationService, ITransientDependency
{
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IPayrollRegisterStore _registerStore;

    public PayrollArrearCalculationService(
        IRepository<WageType> wageTypeRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IPayrollRegisterStore registerStore)
    {
        _wageTypeRepository = wageTypeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _registerStore = registerStore;
    }

    /// <summary>Legacy <c>PayrollArrearService.Main</c> (legacy line 13).</summary>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy line 22. The if-arm's body is entirely commented out AND it is an else-if, so this is a
        // total no-op for the employee -- see class remarks, quirk 1.
        if (context.IsNotReturnFromLeave && context.WageType.BillingType != BillingType.Provision)
        {
            return;
        }

        // Legacy line 29.
        if (context.CurrentSalaryRegisters.Count == 0)
        {
            return;
        }

        var arrearSourceWageType = ResolveArrearSourceWageType(context.WageType);

        // Legacy line 32.
        if (arrearSourceWageType == null || context.PayDate != context.PayInDate)
        {
            return;
        }

        // Legacy line 34.
        var amount = await GetArrearAsync(context, arrearSourceWageType);

        var line = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);

        if (amount == 0)
        {
            // Legacy lines 69-75: an existing row is ZEROED, a missing row is NOT created.
            if (line == null)
            {
                return;
            }

            line.Amount = 0;
            await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);
            return;
        }

        // Legacy lines 49-68. Rate/Number deliberately untouched (legacy lines 57-58 are commented out).
        line ??= new PayrollRegisterLine
        {
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code
        };

        line.Amount = amount;

        await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);
    }

    /// <summary>
    /// Legacy <c>PayrollArrearService.GetArrear</c> (legacy line 84). For each of THIS run's other retro
    /// registers, ADD that register's figure for the arrear SOURCE wage type, then SUBTRACT whatever the
    /// latest previously-paid REGULAR register already paid for the same <c>ForPeriod</c>.
    ///
    /// <para><see cref="BillingType.TimeType"/> source wage types are read from <c>Number</c>; every other
    /// billing type from <c>Amount</c> (legacy lines 111 and 118).</para>
    /// </summary>
    private async Task<double> GetArrearAsync(PayrollCalculationContext context, WageType arrearSourceWageType)
    {
        double arrear = 0;

        // Legacy line 91 -- redundant with the caller's test, kept anyway.
        if (context.PayDate != context.PayInDate)
        {
            return arrear;
        }

        var isTimeType = arrearSourceWageType.BillingType == BillingType.TimeType;

        foreach (var currentSalReg in context.CurrentSalaryRegisters)
        {
            // Legacy lines 95-104. NOTE the run-type gate and `InPeriod != payInDate` -- see class remarks.
            SalaryRegister lastSalReg = null;

            if (context.PayrollRunType == PayrollRunType.Regular || context.IsEndOfService)
            {
                lastSalReg = _salaryRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.ForPeriod == currentSalReg.ForPeriod
                                && r.InPeriod != context.PayInDate
                                && r.PayrollRunType == PayrollRunType.Regular)
                    .OrderByDescending(r => r.InPeriod)
                    .FirstOrDefault();
            }

            // Legacy lines 106-111. A missing line adds 0 rather than skipping the register (legacy line 108).
            var currentLine = await _registerStore.GetLineByWageTypeIdAsync(
                ToRegisterRef(currentSalReg), arrearSourceWageType.Id);

            if (currentLine != null)
            {
                arrear += isTimeType ? currentLine.Number : currentLine.Amount;
            }

            if (lastSalReg == null)
            {
                continue;
            }

            // Legacy lines 113-120.
            var lastLine = await _registerStore.GetLineByWageTypeIdAsync(
                ToRegisterRef(lastSalReg), arrearSourceWageType.Id);

            if (lastLine != null)
            {
                arrear -= isTimeType ? lastLine.Number : lastLine.Amount;
            }
        }

        return arrear;
    }

    /// <summary>
    /// Legacy <c>currWageType.ArrearSourceWageType</c> (legacy line 31). XPO resolved the reference lazily;
    /// here the navigation property may or may not be populated depending on how the wage type was loaded, so
    /// the FK is the authority and the repository is the fallback. Returns <c>null</c> exactly when legacy's
    /// reference was <c>null</c>.
    /// </summary>
    private WageType ResolveArrearSourceWageType(WageType wageType)
    {
        if (wageType.ArrearSourceWageTypeId == null)
        {
            return null;
        }

        if (wageType.ArrearSourceWageType != null)
        {
            return wageType.ArrearSourceWageType;
        }

        var id = wageType.ArrearSourceWageTypeId.Value;
        return _wageTypeRepository.GetAll().FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    /// Same helper shape <see cref="ITimeTypeCumulationService"/> uses: the registers reached through
    /// <see cref="PayrollCalculationContext.CurrentSalaryRegisters"/> and the prior-register query are live
    /// <see cref="SalaryRegister"/> rows, i.e. always <see cref="PayrollRegisterTarget.Live"/>.
    /// </summary>
    private static PayrollRegisterRef ToRegisterRef(SalaryRegister salaryRegister)
    {
        return new PayrollRegisterRef
        {
            RegisterId = salaryRegister.Id,
            Target = PayrollRegisterTarget.Live,
            ForPeriod = salaryRegister.ForPeriod,
            InPeriod = salaryRegister.InPeriod,
            PayrollRunType = salaryRegister.PayrollRunType
        };
    }
}
