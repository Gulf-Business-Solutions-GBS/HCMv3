using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IEmployeePaymentCalculationService"/>
/// <remarks>
/// <b>WAVE P9.</b> Ported from legacy <c>HCM10.PayrollEngine/EmployeePaymentService.cs</c>: <c>Main</c>
/// (legacy line 14) and its <c>GetAmountFromUnitType</c> (legacy line 310).
/// <c>Main_Simu</c>/<c>GetAmountFromUnitType_Simu</c> (legacy lines 398-766) are deliberately NOT ported --
/// wave P7 collapses the simulation clone through <see cref="PayrollRegisterRef.Target"/>.
///
/// <para>Dispatched at legacy <c>Program.cs</c> line 2549. The earning/deduction PAIR gate (legacy line 2547)
/// is applied by the ENGINE, not here.</para>
///
/// <para><b>Four independent contributions, in legacy's order</b>, all accumulating onto ONE register line:</para>
/// <list type="number">
/// <item><description><b>Employee payments</b> (legacy lines 40-138): month-by-month over the recovery
/// periods -- <c>UnitType.Amount</c> rows summed directly, <c>UnitType.Day</c>/<c>Hour</c> rows converted by
/// <c>GetAmountFromUnitType</c>.</description></item>
/// <item><description><b>Employee sub-group payments with <c>PayInterval.Yearly</c></b> (legacy lines 82-107),
/// matched on the sub-group covering that month's END and on the month number.</description></item>
/// <item><description><b>Employee sub-group payments with <c>PayInterval.EndOfService</c></b> (legacy lines
/// 140-170), only on an End-Of-Service off-cycle non-retro register.</description></item>
/// <item><description><b>Recurring payments</b> (legacy lines 202-301), written as a SECOND accumulation onto
/// the same line after the first three have already been saved.</description></item>
/// </list>
///
/// <para><b>Preserved legacy quirks -- all deliberate, none "fixed":</b></para>
/// <list type="number">
/// <item><description><b>The <c>IsNotReturnFromLeave</c> arm is EMPTY and swallows the calculation</b> (legacy
/// line 23), with no <c>BillingType != Provision</c> escape -- same shape as
/// <see cref="EmployeeAttendancePayCalculationService"/> and unlike
/// <see cref="PayrollArrearCalculationService"/>.</description></item>
/// <item><description><b><c>Amount</c> is ACCUMULATED (<c>+=</c>), not assigned</b> (legacy lines 185 and
/// 284), while <c>Number</c> and <c>Rate</c> are hard-zeroed on every write (legacy lines 183-184, 282-283).
/// So a payment wage type that another rule already wrote a Rate onto loses that Rate.</description></item>
/// <item><description><b><c>monthEnddate</c> is clamped to <c>offCycleEndDate</c> on EVERY run type</b>
/// (legacy lines 65-66, 225-228), including Regular -- where <c>offCycleEndDate</c> is simply
/// <c>payInDate</c> (legacy <c>Program.cs</c> line 1607). On a retro period whose month end is AFTER
/// <c>payInDate</c> this narrows the window. Reproduced as written.</description></item>
/// <item><description><b>The "does this wage type have any sub-group payment?" pre-check is NOT scoped to the
/// employee's sub-group</b> (legacy lines 42-43); only the amount lookup inside the month loop is (legacy line
/// 101). So an unrelated sub-group's configuration can open the month loop for an employee who then earns
/// nothing from it -- harmless, but it also means the <c>GetAmountFromUnitType</c> conversion runs in that
/// case. Reproduced.</description></item>
/// <item><description><b>Yearly RECURRING payments match on MONTH NUMBER only</b> (legacy line 239:
/// <c>r.StartDate.Month == monthStartdate.Month</c>) -- no year comparison, and none of the
/// <c>!(r.StartDate &gt; monthEnddate || r.EndDate &lt; monthStartdate)</c> range check the Monthly branch
/// immediately above applies. So a matching month pays the FULL amount however little of that month the
/// payment's own range covers. NOTE the bound: this operates on an already range-filtered list, because the
/// per-employee load at legacy <c>Program.cs</c> lines 1880-1882 applies
/// <c>!(r.StartDate &gt; offCycleEndDate || r.EndDate &lt; payStartDate)</c> first. A payment whose range lies
/// wholly outside the run window is never loaded, so the quirk does NOT mean "pays forever, including before
/// its own StartDate".</description></item>
/// <item><description><b><c>GetAmountFromUnitType</c> takes the FIRST basic-pay detail's parent to read
/// <c>PayInterval</c></b> (legacy line 340) and returns 0 when the employee has no basic-pay detail row at
/// all, even though <c>payRate</c> was already computed. Reproduced.</description></item>
/// <item><description><b><c>BillingMethod.FixedAmountPerMonth</c> is an empty branch</b> (legacy lines
/// 373-377), leaving <c>perDaySalary</c> at 0 -- i.e. Day/Hour unit payments on such a wage type pay
/// nothing.</description></item>
/// <item><description><b>The advance-payment deduction IS sign-flipped here</b> (legacy lines 114, 247:
/// <c>BillingType == Earning ? amount : -amount</c>), unlike
/// <see cref="EmployeeOvertimePayCalculationService"/>.</description></item>
/// <item><description><b>Legacy's <c>Program.calculateSalRegDaysDetail</c> gate is a hardcoded <c>true</c>
/// literal</b> and is not carried -- same decision as elsewhere in the port.</description></item>
/// </list>
///
/// <para><b>No EF Core.</b> Plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>.</para>
/// </remarks>
public class EmployeePaymentCalculationService : HCMv3DomainServiceBase, IEmployeePaymentCalculationService, ITransientDependency
{
    private readonly IRepository<EmployeeRecurringPayment> _employeeRecurringPaymentRepository;
    private readonly IRepository<EmployeeSubGroupPayment> _employeeSubGroupPaymentRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IPayrollWeekOffCalculator _weekOffCalculator;

    public EmployeePaymentCalculationService(
        IRepository<EmployeeRecurringPayment> employeeRecurringPaymentRepository,
        IRepository<EmployeeSubGroupPayment> employeeSubGroupPaymentRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IPayrollWeekOffCalculator weekOffCalculator)
    {
        _employeeRecurringPaymentRepository = employeeRecurringPaymentRepository;
        _employeeSubGroupPaymentRepository = employeeSubGroupPaymentRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _weekOffCalculator = weekOffCalculator;
    }

    /// <summary>Legacy <c>EmployeePaymentService.Main</c> (legacy line 14).</summary>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy line 23 -- empty if-arm, see class remarks quirk 1.
        if (context.IsNotReturnFromLeave)
        {
            return;
        }

        // Legacy lines 32-35.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var wageType = context.WageType;

        // Legacy line 1880 (Program.cs) -- legacy's per-employee empRecPaymentList, which the context does not
        // carry. Same predicate, verbatim.
        var recurringPayments = _employeeRecurringPaymentRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && !(r.StartDate > context.OffCycleEndDate || r.EndDate < context.PayStartDate))
            .ToList();

        // Legacy line 2708 (Program.cs): the WHOLE table, unscoped.
        var subGroupPayments = _employeeSubGroupPaymentRepository.GetAll().ToList();

        double totalAmount = 0;
        var monthWiseDays = new List<SalaryRegisterDayCount>();

        // ------------------------------------------------------------------------------------------------
        // Legacy lines 40-138: EmployeePayment + EmployeeSubGroup (Yearly) payments.
        // ------------------------------------------------------------------------------------------------

        var hasEmployeePayments = context.PaymentList.Any(r => r.WageTypeId == wageType.Id);
        var hasSubGroupPayments = subGroupPayments.Any(r => r.WageTypeId == wageType.Id
                                                            && (r.PayInterval == PayInterval.Yearly
                                                                || r.PayInterval == PayInterval.EndOfService));

        if (hasEmployeePayments || hasSubGroupPayments)
        {
            // Legacy lines 47-52.
            var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
            var payEndDate = context.PayDate;

            if (context.PayrollRunType == PayrollRunType.OffCycle
                && context.Register.InPeriod == context.Register.ForPeriod)
            {
                payEndDate = context.OffCycleEndDate;
            }

            var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(payStartDate, payEndDate);

            foreach (var recoveryPeriod in recoveryPeriods)
            {
                // Legacy lines 63-66 -- clamp applies on EVERY run type, see class remarks quirk 3.
                var monthStartDate = recoveryPeriod;
                var monthEndDate = PayrollRunGates.MonthEnd(recoveryPeriod);

                if (monthEndDate > context.OffCycleEndDate)
                {
                    monthEndDate = context.OffCycleEndDate;
                }

                // Legacy lines 70-74.
                var amount = context.PaymentList
                    .Where(r => r.WageTypeId == wageType.Id
                                && r.PaymentDate >= monthStartDate
                                && r.PaymentDate <= monthEndDate
                                && r.UnitType == UnitType.Amount)
                    .Sum(r => r.Amount);

                // Legacy lines 76-79. NOTE legacy passes monthStartdate as the `payDate` argument, so every
                // "days in month" / week-off computation inside runs against the RECOVERY month, not payDate.
                amount += await GetAmountFromUnitTypeAsync(context, monthStartDate, monthStartDate, monthEndDate);

                // Legacy lines 83-106.
                var employeeSubGroupId = context.OrgAssignments
                    .Where(r => r.StartDate <= monthEndDate && r.EndDate >= monthEndDate)
                    .Select(r => r.EmployeeSubGroupId)
                    .FirstOrDefault();

                if (employeeSubGroupId != null)
                {
                    var payMonth = (CustomStartMonth)monthEndDate.Month;

                    amount += subGroupPayments
                        .Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value
                                    && r.WageTypeId != null
                                    && r.WageTypeId == wageType.Id
                                    && r.PayInterval == PayInterval.Yearly
                                    && r.PayMonth == payMonth)
                        .Sum(r => r.Amount);
                }

                if (amount == 0)
                {
                    continue;
                }

                // Legacy lines 112-116 -- sign-flipped, see class remarks quirk 8.
                if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
                {
                    var advanceAmount = wageType.BillingType == BillingType.Earning ? amount : -1 * amount;
                    await AdvancePaymentDedAsync(context, recoveryPeriod, advanceAmount);
                }

                totalAmount += amount;

                // Legacy lines 122-132.
                AddMonthWiseInfo(context, monthWiseDays, recoveryPeriod, amount);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Legacy lines 140-170: EmployeeSubGroupPayment with PayInterval.EndOfService. NOTE this block sits
        // OUTSIDE the `hasEmployeePayments || hasSubGroupPayments` guard above.
        // ------------------------------------------------------------------------------------------------

        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.IsEndOfService
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            var employeeSubGroupId = context.OrgAssignments
                .Where(r => r.StartDate <= context.PayDate && r.EndDate >= context.PayDate)
                .Select(r => r.EmployeeSubGroupId)
                .FirstOrDefault();

            if (employeeSubGroupId != null)
            {
                var endOfServiceAmount = subGroupPayments
                    .Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value
                                && r.WageTypeId != null
                                && r.WageTypeId == wageType.Id
                                && r.PayInterval == PayInterval.EndOfService)
                    .Sum(r => r.Amount);

                totalAmount += endOfServiceAmount;

                // Legacy lines 156-166.
                AddMonthWiseInfo(context, monthWiseDays, context.PayDate, endOfServiceAmount);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Legacy lines 172-200: write the accumulated payment amount.
        // ------------------------------------------------------------------------------------------------

        if (totalAmount != 0)
        {
            await AccumulateOntoLineAsync(context, totalAmount, monthWiseDays);
        }

        // ------------------------------------------------------------------------------------------------
        // Legacy lines 202-301: recurring payments, a SECOND accumulation onto the same line.
        // ------------------------------------------------------------------------------------------------

        await CalculateRecurringPaymentsAsync(context, recurringPayments);
    }

    /// <summary>Legacy lines 202-301.</summary>
    private async Task CalculateRecurringPaymentsAsync(
        PayrollCalculationContext context,
        List<EmployeeRecurringPayment> recurringPayments)
    {
        var wageType = context.WageType;

        // Legacy lines 203-204.
        if (!recurringPayments.Any(r => r.WageTypeId == wageType.Id))
        {
            return;
        }

        // Legacy lines 206-211.
        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(payStartDate, payEndDate);

        double totalRecurringAmount = 0;
        var monthWiseDays = new List<SalaryRegisterDayCount>();

        foreach (var recoveryPeriod in recoveryPeriods)
        {
            // Legacy lines 223-228.
            var monthStartDate = recoveryPeriod;
            var monthEndDate = PayrollRunGates.MonthEnd(recoveryPeriod);

            if (monthEndDate > context.OffCycleEndDate)
            {
                monthEndDate = context.OffCycleEndDate;
            }

            // Legacy lines 231-233.
            var recurringAmount = recurringPayments
                .Where(r => r.WageTypeId == wageType.Id
                            && r.PayInterval == PayInterval.Monthly
                            && !(r.StartDate > monthEndDate || r.EndDate < monthStartDate))
                .Sum(r => r.Amount);

            // Legacy lines 237-239 -- month NUMBER only, see class remarks quirk 5.
            recurringAmount += recurringPayments
                .Where(r => r.WageTypeId == wageType.Id
                            && r.PayInterval == PayInterval.Yearly
                            && r.StartDate.Month == monthStartDate.Month)
                .Sum(r => r.Amount);

            if (recurringAmount == 0)
            {
                continue;
            }

            // Legacy lines 245-249.
            if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
            {
                var advanceAmount = wageType.BillingType == BillingType.Earning
                    ? recurringAmount
                    : -1 * recurringAmount;

                await AdvancePaymentDedAsync(context, recoveryPeriod, advanceAmount);
            }

            totalRecurringAmount += recurringAmount;

            // Legacy lines 255-267.
            AddMonthWiseInfo(context, monthWiseDays, recoveryPeriod, recurringAmount);
        }

        // Legacy lines 272-299.
        if (totalRecurringAmount != 0)
        {
            await AccumulateOntoLineAsync(context, totalRecurringAmount, monthWiseDays);
        }
    }

    /// <summary>
    /// Legacy lines 175-198 / 275-297: <c>Number = 0; Rate = 0; Amount += total;</c> then the month-wise rows.
    /// </summary>
    private async Task AccumulateOntoLineAsync(
        PayrollCalculationContext context,
        double amountToAdd,
        List<SalaryRegisterDayCount> monthWiseDays)
    {
        var line = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id)
                   ?? new PayrollRegisterLine
                   {
                       WageTypeId = context.WageType.Id,
                       WageCode = context.WageType.Code
                   };

        line.Number = 0;
        line.Rate = 0;
        line.Amount += amountToAdd;

        // Legacy adds the detail unconditionally (lines 193, 292) -- no "zero means delete" on this arm.
        var saved = await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);

        // Legacy Program.AddSalaryRegisterDaysDetail's row-level guard (legacy Program.cs line 5443).
        var days = monthWiseDays
            .Where(d => d.Rate != 0 || d.Number != 0 || d.Amount != 0)
            .ToList();

        if (saved != null && days.Count > 0)
        {
            await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, days);
        }
    }

    /// <summary>
    /// Legacy's "MonthWiseInfo" sub-block (lines 122-132, 156-166, 255-267): off-cycle non-retro registers
    /// only, non-zero amounts only, stamped with the month's LAST day and <c>Rate = Number = 0</c>.
    /// </summary>
    private static void AddMonthWiseInfo(
        PayrollCalculationContext context,
        List<SalaryRegisterDayCount> monthWiseDays,
        DateTime periodSeed,
        double amount)
    {
        if (context.PayrollRunType != PayrollRunType.OffCycle
            || context.Register.InPeriod != context.Register.ForPeriod
            || amount == 0)
        {
            return;
        }

        monthWiseDays.Add(new SalaryRegisterDayCount
        {
            Period = PayrollRunGates.MonthEnd(periodSeed),
            Rate = 0,
            Number = 0,
            Amount = amount
        });
    }

    /// <summary>
    /// Legacy <c>EmployeePaymentService.GetAmountFromUnitType</c> (legacy line 310): converts
    /// <see cref="UnitType.Day"/> / <see cref="UnitType.Hour"/> payment rows into an amount using the
    /// employee's <see cref="WageType.IncludeInPartialPay"/> basic-pay total and the current wage type's
    /// <see cref="WageType.BillingMethod"/>.
    ///
    /// <para><paramref name="payDate"/> is legacy's <c>payDate</c> PARAMETER, which the caller supplies as the
    /// recovery month's FIRST day (legacy line 76), not the run's pay date.</para>
    /// </summary>
    private async Task<double> GetAmountFromUnitTypeAsync(
        PayrollCalculationContext context,
        DateTime payDate,
        DateTime monthStartDate,
        DateTime monthEndDate)
    {
        var wageType = context.WageType;

        // Legacy lines 322-332.
        double totalDays = 0;

        var dayHourPayments = context.PaymentList
            .Where(r => r.WageTypeId == wageType.Id
                        && r.PaymentDate >= monthStartDate
                        && r.PaymentDate <= monthEndDate
                        && (r.UnitType == UnitType.Day || r.UnitType == UnitType.Hour))
            .ToList();

        foreach (var payment in dayHourPayments)
        {
            if (payment.UnitType == UnitType.Day)
            {
                totalDays += payment.Amount;
            }
            else if (payment.UnitType == UnitType.Hour && context.Settings.DefaultWorkingHours != 0)
            {
                totalDays += payment.Amount / context.Settings.DefaultWorkingHours;
            }
        }

        // Legacy line 336.
        if (totalDays == 0)
        {
            return 0;
        }

        // Legacy lines 339-340. Legacy's empBasicPayDetailList is the detail set of the basic-pay row covering
        // payDate (legacy Program.cs lines 1918-1922) -- note it is built from the RUN's payDate, not from the
        // recovery month, so the same predicate against context.PayDate is used here.
        var basicPayDetails = GetBasicPayDetails(context.EmployeeId, context.PayDate);

        var includeInPartialPayWageTypeIds = _wageTypeRepository.GetAll()
            .Where(r => r.IncludeInPartialPay)
            .Select(r => r.Id)
            .ToList();

        var payRate = basicPayDetails
            .Where(r => includeInPartialPayWageTypeIds.Contains(r.WageTypeId))
            .Sum(r => r.Amount);

        var firstDetail = basicPayDetails.FirstOrDefault();
        if (firstDetail == null)
        {
            // Legacy line 342: `if (empBasicPay != null)` -- no basic pay, no amount. Quirk 6.
            return 0;
        }

        var employeeBasicPay = _employeeBasicPayRepository.GetAll()
            .FirstOrDefault(r => r.Id == firstDetail.EmployeeBasicPayId);

        if (employeeBasicPay == null)
        {
            return 0;
        }

        double perDaySalary = 0;

        if (employeeBasicPay.PayInterval == PayInterval.Monthly)
        {
            if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
            {
                // Legacy lines 349-350.
                double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                perDaySalary = payRate / calendarDays;
            }
            else if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
            {
                // Legacy lines 357-361.
                double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                var weeklyOffDays = await _weekOffCalculator.GetActualWeekOffsAsync(context, payDate);
                var salaryBasisDays = calendarDays - weeklyOffDays;
                perDaySalary = payRate / salaryBasisDays;
            }
            else if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
            {
                // Legacy line 368.
                perDaySalary = payRate / wageType.FixedDays;
            }

            // BillingMethod.FixedAmountPerMonth: legacy lines 373-377 are an EMPTY branch -- quirk 7.
        }
        else if (employeeBasicPay.PayInterval == PayInterval.Daily)
        {
            // Legacy line 381.
            perDaySalary = payRate;
        }

        // Legacy line 384.
        return perDaySalary * totalDays;
    }

    /// <summary>
    /// Legacy <c>Program.cs</c> lines 1918-1922, the same shape
    /// <see cref="EmployeeBasicPayCalculationService"/> ports: the basic-pay row covering
    /// <paramref name="payDate"/> (unordered <c>FirstOrDefault</c>, exactly as legacy) and its details.
    /// </summary>
    private List<EmployeeBasicPayDetail> GetBasicPayDetails(int employeeId, DateTime payDate)
    {
        var basicPay = _employeeBasicPayRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate);

        if (basicPay == null)
        {
            return new List<EmployeeBasicPayDetail>();
        }

        return _employeeBasicPayDetailRepository.GetAll()
            .Where(r => r.EmployeeBasicPayId == basicPay.Id)
            .ToList();
    }

    /// <summary>Legacy <c>Program.AdvancePaymentDed</c>, reached from legacy lines 115 and 248.</summary>
    private Task AdvancePaymentDedAsync(PayrollCalculationContext context, DateTime paymentDate, double amount)
    {
        if (amount == 0)
        {
            return Task.CompletedTask;
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService)
        {
            return Task.CompletedTask;
        }

        return _advancePaymentService.AdvancePaymentDedAsync(
            context.EmployeeId,
            paymentDate,
            context.OffCyclePaymentDate,
            amount,
            context.PayrollRunType,
            context.OffCycleReason,
            context.AdvancePayComment,
            context.Settings);
    }
}
