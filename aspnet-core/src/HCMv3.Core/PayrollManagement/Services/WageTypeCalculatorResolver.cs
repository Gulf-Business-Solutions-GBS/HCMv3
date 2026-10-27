using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IWageTypeCalculatorResolver"/>
/// <remarks>
/// Same strategy+resolver shape as the sibling TimeEngine wave's
/// <c>HCMv3.TimeManagement.Services.PunchResolutionStrategyResolver</c>: implementations are injected as
/// <c>IEnumerable&lt;IWageTypeCalculator&gt;</c> (ABP/Castle conventional registration resolves all of them)
/// and matched on <see cref="IWageTypeCalculator.WageCode"/>, so adding a rule requires no change here.
///
/// <para><b>Unclaimed wage codes are normal.</b> Legacy iterated every active non-TimeType wage type ordered
/// by <c>PayrollOrder</c> (legacy <c>Program.cs</c> line 2524) and fell straight through its <c>else if</c>
/// chain for codes it had no branch for. <see cref="Resolve"/> therefore returns <c>null</c> rather than
/// throwing. What it DOES throw on is two calculators claiming the same code -- an ambiguity legacy could not
/// even express.</para>
///
/// <para><b>The four gates live here, not in the rules.</b> See <see cref="IWageTypeCalculatorResolver"/> for
/// the enumerated list and legacy line references. Concentrating them here is the whole point: the legacy
/// source had ~34 hand-copied instances of the same <c>if (!IsOnHold(...))</c> wrapper, and the copies had
/// already begun to diverge (e.g. Air Ticket Pay tests <c>onHold_AirTicketPay</c> INSTEAD of calling
/// <c>IsOnHold</c>, Indemnity Pay tests <c>onHold_IndemnityPay</c> instead, and the 3000/3003 Provision
/// branches test NEITHER). Those specific per-hold divergences are NOT swept into the generic gate -- they
/// remain the individual rule's business via <see cref="PayrollCalculationContext.OnHold"/>, exactly as
/// legacy has them.</para>
/// </remarks>
[UnitOfWork]
public class WageTypeCalculatorResolver : HCMv3DomainServiceBase, IWageTypeCalculatorResolver
{
    private readonly List<IWageTypeCalculator> _calculators;
    private readonly IRepository<WageTypeException> _wageTypeExceptionRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeRejoin> _employeeRejoinRepository;

    public WageTypeCalculatorResolver(
        IEnumerable<IWageTypeCalculator> calculators,
        IRepository<WageTypeException> wageTypeExceptionRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeRejoin> employeeRejoinRepository)
    {
        _calculators = calculators?.ToList() ?? new List<IWageTypeCalculator>();
        _wageTypeExceptionRepository = wageTypeExceptionRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeRejoinRepository = employeeRejoinRepository;
    }

    /// <inheritdoc/>
    public IWageTypeCalculator Resolve(string wageCode)
    {
        if (string.IsNullOrEmpty(wageCode))
        {
            return null;
        }

        // IMasterDataDispatchedWageTypeCalculator implementations declare WageCode == null on purpose: they
        // are dispatched by master-table reference, never by code. The null never equals a real code, so
        // they are naturally excluded here; the OfType filter is kept explicit so it stays that way.
        var matches = _calculators
            .Where(c => c is not IMasterDataDispatchedWageTypeCalculator && c.WageCode == wageCode)
            .ToList();

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"More than one IWageTypeCalculator is registered for WageType code '{wageCode}': "
                + string.Join(", ", matches.Select(m => m.GetType().FullName))
                + ". Wage codes must be unique -- the legacy engine expressed each as a single 'else if' branch, "
                + "so there is no defined precedence to fall back on.");
        }

        return matches.FirstOrDefault();
    }

    /// <inheritdoc/>
    public bool HasCalculatorFor(WageType wageType)
    {
        if (wageType == null)
        {
            return false;
        }

        return Resolve(wageType.Code) != null || MasterDataClaimants(wageType.Id).Any();
    }

    /// <summary>
    /// Legacy's four master-table membership tests (<c>Program.cs</c> lines 2698-2701, re-applied inside
    /// <c>EmployeeCalculatedPayService.Main</c> at lines 45, 59, 73, 119). ALL claimants run -- legacy's
    /// regions are sequential blocks, not an else-if chain.
    /// </summary>
    private IEnumerable<IMasterDataDispatchedWageTypeCalculator> MasterDataClaimants(int wageTypeId)
    {
        return _calculators
            .OfType<IMasterDataDispatchedWageTypeCalculator>()
            .Where(c => c.ClaimsWageType(wageTypeId));
    }

    /// <inheritdoc/>
    public async Task<bool> TryCalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set before dispatching.", nameof(context));
        }

        // Gate 1 -- legacy Program.Allow_CalcWageType (Program.cs lines 2752-2770).
        if (!AllowCalcWageType(context))
        {
            return false;
        }

        // Gate 2 -- legacy EmployeeCalculatedPayService.cs line 35.
        if (!AllowWhenNotReturnFromLeave(context))
        {
            return false;
        }

        // Gate 3 -- legacy EmployeeCalculatedPayService.IsOnHold (line 1160).
        if (IsOnHold(context.WageType, context.OnHold))
        {
            return false;
        }

        // Gate 4 -- legacy EmployeeCalculatedPayService.AllowToCalculate (line 1164).
        if (!AllowToCalculate(context))
        {
            return false;
        }

        var ranSomething = false;

        // Legacy EmployeeCalculatedPayService.Main runs its master-table regions FIRST (lines 44-130), then
        // its by-code branches (line 131 onward). Order preserved: a by-code rule that reads register lines
        // must see what the master-table rules already posted, exactly as legacy did.
        // ToList() because a rule may write rows the claim predicate itself queries.
        foreach (var claimant in MasterDataClaimants(context.WageType.Id).ToList())
        {
            await claimant.CalculateAsync(context);
            ranSomething = true;
        }

        var calculator = Resolve(context.WageType.Code);
        if (calculator != null)
        {
            await calculator.CalculateAsync(context);
            ranSomething = true;
        }

        return ranSomething;
    }

    /// <summary>
    /// Legacy <c>EmployeeCalculatedPayService.IsOnHold</c> (legacy
    /// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 1160-1163), verbatim:
    /// <c>(BillingType == Earning &amp;&amp; onHold_AllEarnings) || (BillingType == Deduction &amp;&amp;
    /// onHold_AllDeductions)</c>. Note it deliberately says NOTHING about Provision/TimeType wage types --
    /// those are never gated by it.
    /// </summary>
    public static bool IsOnHold(WageType wageType, PayrollOnHoldFlags onHold)
    {
        if (wageType == null || onHold == null)
        {
            return false;
        }

        return (wageType.BillingType == BillingType.Earning && onHold.AllEarnings)
               || (wageType.BillingType == BillingType.Deduction && onHold.AllDeductions);
    }

    /// <summary>
    /// Legacy <c>EmployeeCalculatedPayService.Main</c>'s outermost gate (legacy line 35): when the employee
    /// has an unresolved "not returned from leave" record, the entire calculated-pay body is skipped for every
    /// wage type EXCEPT <c>BillingType.Provision</c> ones and 9000 Net Pay.
    /// </summary>
    private static bool AllowWhenNotReturnFromLeave(PayrollCalculationContext context)
    {
        if (!context.IsNotReturnFromLeave)
        {
            return true;
        }

        return context.WageType.BillingType == BillingType.Provision
               || context.WageType.Code == PayrollEngineWageCodes.NetPay;
    }

    /// <summary>
    /// Legacy <c>Program.Allow_CalcWageType</c> (legacy <c>Program.cs</c> lines 2752-2770): if this wage type
    /// has <see cref="WageTypeException"/> rows, skip it when ANY of the excepted wage types already has a
    /// line on this register with a non-zero Rate, Number OR Amount.
    /// </summary>
    private bool AllowCalcWageType(PayrollCalculationContext context)
    {
        var exceptWageTypeIds = _wageTypeExceptionRepository.GetAll()
            .Where(e => e.WageTypeId == context.WageType.Id)
            .Select(e => e.ExceptWageTypeId)
            .ToList();

        if (!exceptWageTypeIds.Any())
        {
            return true;
        }

        if (context.Register == null)
        {
            return true;
        }

        if (context.Register.Target == PayrollRegisterTarget.Simulation)
        {
            // Legacy SimulationPayrollHelper.Allow_CalcWageType (line 2258): the same test on the simulation
            // register's lines.
            var simulation = SimulationRows ?? throw new InvalidOperationException(
                "WageTypeCalculatorResolver needs ISimulationRegisterRows to read a simulation register.");

            return !simulation.Details
                .Any(d => d.SimulationSalaryRegisterId == context.Register.RegisterId
                          && exceptWageTypeIds.Contains(d.WageTypeId)
                          && (d.Rate != 0 || d.Number != 0 || d.Amount != 0));
        }

        var blocked = _salaryRegisterDetailRepository.GetAll()
            .Any(d => d.SalaryRegisterId == context.Register.RegisterId
                      && exceptWageTypeIds.Contains(d.WageTypeId)
                      && (d.Rate != 0 || d.Number != 0 || d.Amount != 0));

        return !blocked;
    }

    /// <summary>The simulation tables. Property-injected so the hand-built test resolvers keep compiling.</summary>
    public ISimulationRegisterRows SimulationRows { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeCalculatedPayService.AllowToCalculate</c> (legacy lines 1164-1197).
    ///
    /// <para>Legacy semantics, preserved exactly: find the most recent <c>EmployeeRejoin</c> starting on or
    /// before the pay date (ignoring rows whose <c>StartDate</c> is the SQL minimum sentinel, legacy line
    /// 1172); if there is none, take any un-revoked <c>EmployeeSeparation</c>, otherwise take one whose
    /// <c>LastDayInService</c> is on or after the rejoin's start date. If such a separation exists and the
    /// MONTH END of its <c>LastDayInService</c> is before the pay date, the wage type is not calculated.</para>
    ///
    /// <para>Legacy's <c>payrollRunType</c>/<c>offCycleEndDate</c>/<c>offCycleReasons</c> parameters are
    /// declared but never read in the legacy body -- not ported. Legacy also swallows any exception and
    /// returns <c>true</c> (lines 1192-1195); preserved.</para>
    ///
    /// <para>Legacy passes <c>payEndDate</c> (its two call sites are at legacy lines 3022 and 6952), which is
    /// the period end -- <see cref="PayrollCalculationContext.PayDate"/> here.</para>
    ///
    /// <para><b>PROPERTY RENAME:</b> legacy reads <c>EmployeeRejoin.StartDate</c>; this port's
    /// <see cref="EmployeeRejoin"/> calls the same field <c>RejoinDate</c> (verified against the entity, not
    /// assumed).</para>
    /// </summary>
    private bool AllowToCalculate(PayrollCalculationContext context)
    {
        try
        {
            var payDate = context.PayDate;

            // Legacy line 1172 guards against SqlDateTime.MinValue (1753-01-01); this port stores unset dates
            // as default(DateTime), so both sentinels are excluded.
            var sqlMinDate = new DateTime(1753, 1, 1);

            var rejoin = _employeeRejoinRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.RejoinDate != default(DateTime)
                            && r.RejoinDate != sqlMinDate
                            && r.RejoinDate <= payDate)
                .OrderByDescending(r => r.RejoinDate)
                .FirstOrDefault();

            EmployeeSeparation separation;
            if (rejoin == null)
            {
                separation = _employeeSeparationRepository.GetAll()
                    .FirstOrDefault(s => s.EmployeeId == context.EmployeeId && s.RevokeDate == null);
            }
            else
            {
                separation = _employeeSeparationRepository.GetAll()
                    .FirstOrDefault(s => s.EmployeeId == context.EmployeeId
                                         && s.RevokeDate == null
                                         && s.LastDayInService >= rejoin.RejoinDate);
            }

            if (separation == null)
            {
                return true;
            }

            var allowUptoDate = new DateTime(
                separation.LastDayInService.Year,
                separation.LastDayInService.Month,
                DateTime.DaysInMonth(separation.LastDayInService.Year, separation.LastDayInService.Month));

            return allowUptoDate >= payDate;
        }
        catch (Exception)
        {
            // Legacy lines 1192-1195: exception swallowed, allowToCalculate stays true.
            return true;
        }
    }
}
