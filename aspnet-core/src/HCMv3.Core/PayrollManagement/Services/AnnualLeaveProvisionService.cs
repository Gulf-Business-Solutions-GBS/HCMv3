using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IAnnualLeaveProvisionService"/>
/// <remarks>
/// <para>Wave P5. Ported from legacy <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c>:
/// <c>GetAnnualLeaveProvision</c> (2281), <c>GetTotalAnnualLeaveProvision</c> (2317),
/// <c>CalculateAnnualLeaveProvision</c> (2356), <c>GetAnnualLeaveProvisionDayWiseEntry</c> (2422),
/// <c>GetProvisionChangeAnnualLeave</c> (2531). The <c>*_Simu</c> clones (6250/6281/6380/6483) are NOT ported
/// -- wave P7 collapses them by flipping <see cref="PayrollRegisterRef.Target"/>, which is why every register
/// read/write here goes through <see cref="IPayrollRegisterStore"/>.</para>
///
/// <para><b>Deviations from legacy, all deliberate:</b></para>
/// <list type="bullet">
/// <item><description><b>No EF Core in HCMv3.Core.</b> Plain synchronous LINQ over
/// <c>IRepository&lt;T&gt;.GetAll()</c>, same as <see cref="AccrualService"/> and
/// <see cref="IndemnityCalculationService"/>.</description></item>
/// <item><description><b>No <c>NullReferenceException</c> when no annual-leave AbsenceType exists.</b> Legacy
/// <c>CalculateAnnualLeaveProvision</c> (line 2381-2382) does
/// <c>AbsenceTypeList.Where(r =&gt; r.Code == "1").FirstOrDefault()</c> and then immediately dereferences
/// <c>.Oid</c>, so a database without an AbsenceType coded "1" crashes the employee. This port resolves the
/// id with <c>FirstOrDefault</c> semantics (0 when absent), which makes the paid-leave wage-type list empty
/// and the absence-adjustment filter match nothing -- i.e. a 0 provision instead of a crash. Any real
/// installation has the row, so this only changes the failure mode.</description></item>
/// <item><description><b><c>LeaveAdjDay</c> is non-nullable in this port</b>, so legacy's
/// <c>(r.LeaveAdjDay == null ? 0 : r.LeaveAdjDay)</c> (line 2401) collapses to <c>r.LeaveAdjDay</c>. Same
/// arithmetic.</description></item>
/// <item><description><b>Zero-row creation in the day-wise entry not reproduced.</b> When the "3001" line is
/// absent AND the provision-change difference comes out 0, legacy's
/// <c>objectSpace.CreateObject&lt;SalaryRegisterDetail&gt;()</c> (line 2465) leaves a tracked all-zero row
/// that XAF commits at the end of the run. This port creates the line only on the write path, matching the
/// "zero means delete" convention <see cref="IPayrollRegisterStore.SaveLineAsync"/> enforces everywhere else.
/// Effect: no all-zero 3001 rows.</description></item>
/// <item><description><b>Legacy <c>throw ex;</c> re-throw wrappers dropped</b> (they only destroyed stack
/// traces), and legacy's <c>SysCreator</c>/<c>SysCreatedOn</c>/<c>IsSystemGenerated</c> stamping is dropped
/// for the reasons already documented on <see cref="IndemnityCalculationService"/>.</description></item>
/// </list>
///
/// <para><b>Rounding.</b> Exactly one <c>Math.Round</c> exists in this whole scope --
/// <c>Math.Round(salaryInProvision / 26, 6)</c> in <c>GetProvisionChangeAnnualLeave</c> (legacy line 2591) --
/// and it is reproduced with legacy's literal divisor 26 and literal 6 digits. Nothing else is rounded: the
/// provision amounts are returned at full <c>double</c> precision exactly as legacy produced them, including
/// the division by <c>WageType.FixedDays</c> which legacy does not guard against 0.</para>
/// </remarks>
[UnitOfWork]
public class AnnualLeaveProvisionService : HCMv3DomainServiceBase, IAnnualLeaveProvisionService
{
    /// <summary>Legacy: monthly annual-leave days source line (legacy line 2287).</summary>
    private const string WageCodeAnnualLeaveDays = "4010";

    /// <summary>Legacy: closing annual-leave balance days source line (legacy line 2324).</summary>
    private const string WageCodeAnnualLeaveBalanceDays = "4022";

    /// <summary>Legacy: the monthly annual-leave provision line the day-wise entry operates on (legacy line 2430).</summary>
    private const string WageCodeAnnualLeaveProvision = "3001";

    /// <summary>Legacy: the closing annual-leave provision line read from the previous month (legacy line 2566).</summary>
    private const string WageCodeTotalAnnualLeaveProvision = "3004";

    /// <summary>Legacy <c>Program.AnnualLeaveOid</c> = the AbsenceType whose Code is the literal "1" (legacy Program.cs line 2710).</summary>
    private const string AbsenceTypeCodeAnnualLeave = "1";

    private readonly IPayrollRegisterStore _registerStore;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;

    public AnnualLeaveProvisionService(
        IPayrollRegisterStore registerStore,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<TimeRegister> timeRegisterRepository)
    {
        _registerStore = registerStore;
        _salaryRegisterRepository = salaryRegisterRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeRepository = employeeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _timeRegisterRepository = timeRegisterRepository;
    }

    #region << GetAnnualLeaveProvision / GetTotalAnnualLeaveProvision (legacy 2281 / 2317) >>

    /// <inheritdoc/>
    public Task<AnnualLeaveProvisionAmounts> GetAnnualLeaveProvisionAsync(PayrollCalculationContext context)
    {
        // Legacy line 2285-2286: payStartDate = month start, payEndDate = payDate.
        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1);

        return CalculateAsync(context, WageCodeAnnualLeaveDays, payStartDate);
    }

    /// <inheritdoc/>
    public Task<AnnualLeaveProvisionAmounts> GetTotalAnnualLeaveProvisionAsync(PayrollCalculationContext context)
    {
        // Legacy line 2340: in period-wise mode the total variant restarts the walk at the joining date.
        // (In the non-period-wise arm payStartDate is unused, exactly as in legacy.)
        var joiningDate = context.Employee != null
            ? context.Employee.JoiningDate
            : _employeeRepository.GetAll().Where(e => e.Id == context.EmployeeId).Select(e => e.JoiningDate).FirstOrDefault();

        return CalculateAsync(context, WageCodeAnnualLeaveBalanceDays, joiningDate);
    }

    /// <summary>
    /// Shared body of legacy <c>GetAnnualLeaveProvision</c> (2281) and <c>GetTotalAnnualLeaveProvision</c>
    /// (2317). The two legacy methods are line-for-line identical apart from the source wage code and the
    /// period-wise start date, so they are folded into one body rather than duplicated.
    /// </summary>
    private async Task<AnnualLeaveProvisionAmounts> CalculateAsync(PayrollCalculationContext context, string sourceWageCode, DateTime periodWiseStartDate)
    {
        var result = new AnnualLeaveProvisionAmounts();

        var lines = await _registerStore.GetLinesAsync(context.Register);

        // Legacy 2287 / 2324: FirstOrDefault on the source line; null => zero everything (legacy 2305-2310).
        var sourceLine = lines.FirstOrDefault(l => l.WageCode == sourceWageCode);
        if (sourceLine == null)
        {
            return result;
        }

        var annualLeaveAbsenceTypeId = ResolveAnnualLeaveAbsenceTypeId(context);
        var includeInPaidLeaveWageTypeIds = GetPaidLeaveWageTypeIds(annualLeaveAbsenceTypeId);

        if (!context.Settings.IsPeriodwiseProvision)
        {
            // Legacy 2295-2298 / 2333-2336.
            var totalBasicPay = lines.Where(l => includeInPaidLeaveWageTypeIds.Contains(l.WageTypeId)).Sum(l => l.Rate);

            result.Rate = totalBasicPay;
            result.Number = sourceLine.Number;

            // Legacy divides by the CURRENT wage type's FixedDays with no zero guard. Preserved.
            result.Amount = (totalBasicPay / context.WageType.FixedDays) * sourceLine.Number;
            return result;
        }

        // Legacy 2302 / 2341.
        return CalculateAnnualLeaveProvision(context, periodWiseStartDate, context.PayDate, annualLeaveAbsenceTypeId, includeInPaidLeaveWageTypeIds);
    }

    /// <summary>
    /// Legacy <c>CalculateAnnualLeaveProvision</c> (line 2356): walks the employee's basic-pay records that
    /// overlap the window, and for each sub-period accrues
    /// <c>(basicPay / FixedDays) * (accrual - leaveUsed - absenceAdjustments)</c>.
    /// </summary>
    private AnnualLeaveProvisionAmounts CalculateAnnualLeaveProvision(
        PayrollCalculationContext context,
        DateTime payStartDate,
        DateTime payEndDate,
        int annualLeaveAbsenceTypeId,
        List<int> includeInPaidLeaveWageTypeIds)
    {
        var result = new AnnualLeaveProvisionAmounts();

        double number = 0;
        double amount = 0;

        var employeeId = context.EmployeeId;

        // Legacy 2361-2362: records overlapping either end of the window.
        var overlapping = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && ((r.StartDate <= payStartDate && r.EndDate >= payStartDate)
                            || (r.StartDate <= payEndDate && r.EndDate >= payEndDate)))
            .OrderBy(r => r.StartDate)
            .ToList();

        if (overlapping.Count != 0)
        {
            // Legacy 2366-2378: widen to every basic-pay record fully inside [min(Start), max(End)] of the
            // overlapping set -- i.e. pick up the mid-window records the overlap test alone misses.
            var minStartDate = overlapping.Min(r => r.StartDate);
            var maxEndDate = overlapping.Max(r => r.EndDate);

            var empBasicPayList = _employeeBasicPayRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.StartDate >= minStartDate && r.EndDate <= maxEndDate)
                .OrderBy(r => r.StartDate)
                .ToList();

            var countStartDate = payStartDate;

            foreach (var empBasicPay in empBasicPayList)
            {
                // Legacy 2389.
                var countEndDate = empBasicPay.EndDate <= payEndDate ? empBasicPay.EndDate : payEndDate;
                var basicPayId = empBasicPay.Id;
                var rangeStart = countStartDate;

                // Legacy 2391.
                var totalBasicPay = _employeeBasicPayDetailRepository.GetAll()
                    .Where(r => r.EmployeeBasicPayId == basicPayId && includeInPaidLeaveWageTypeIds.Contains(r.WageTypeId))
                    .Sum(r => r.Amount);

                // Legacy 2393-2401. Two separate legacy queries over the same window; kept separate so the
                // (identical) filters stay line-comparable with the legacy source.
                var totalAccrualInPeriod = _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.DayDate >= rangeStart && r.DayDate <= countEndDate)
                    .Sum(r => r.AnnualLeaveAccrualPerDay);

                var totalLeaveUsed = _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.DayDate >= rangeStart && r.DayDate <= countEndDate)
                    .Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);

                // Legacy 2403-2406.
                var totalAbsAdjust = _employeeAbsAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId
                                && r.AbsenceTypeId == annualLeaveAbsenceTypeId
                                && r.EffectiveDate >= rangeStart
                                && r.EffectiveDate <= countEndDate
                                && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                    .Sum(r => r.QuotaAdjusted);

                var totalAccrualBalance = totalAccrualInPeriod - totalLeaveUsed - totalAbsAdjust;

                // Legacy 2410: no zero guard on FixedDays. Preserved.
                var totalAmountInPeriod = (totalBasicPay / context.WageType.FixedDays) * totalAccrualBalance;

                countStartDate = countEndDate.AddDays(1);
                number += totalAccrualBalance;
                amount += totalAmountInPeriod;
            }
        }

        // Legacy 2418-2420: Rate is explicitly zeroed on the period-wise path.
        result.Rate = 0;
        result.Number = number;
        result.Amount = amount;
        return result;
    }

    #endregion

    #region << GetAnnualLeaveProvisionDayWiseEntry (legacy 2422) >>

    /// <inheritdoc/>
    public async Task<AnnualLeaveProvisionDayWiseResult> GetAnnualLeaveProvisionDayWiseEntryAsync(PayrollCalculationContext context)
    {
        var result = new AnnualLeaveProvisionDayWiseResult();

        var payDate = context.PayDate;
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);
        var payEndDate = payDate;
        var employeeId = context.EmployeeId;

        // Legacy 2429-2441: delete any pre-existing day-wise rows for the 3001 line FIRST. Legacy leaves them
        // deleted on every early-return path below, so this is done before the early returns.
        var provisionLine = await _registerStore.GetLineAsync(context.Register, WageCodeAnnualLeaveProvision);
        if (provisionLine != null)
        {
            await _registerStore.SaveDayDetailsAsync(context.Register, provisionLine.Id, new List<SalaryRegisterDayCount>());
        }

        // Legacy 2444-2448: the first basic-pay record STARTING inside the period; none => nothing to do.
        var incrementDate = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate >= payStartDate && r.StartDate <= payEndDate)
            .Select(r => r.StartDate)
            .FirstOrDefault();

        if (incrementDate == default(DateTime))
        {
            return result;
        }

        // Legacy 2451-2452: there must be an earlier basic pay for there to be a "change".
        var hasIncrement = _employeeBasicPayRepository.GetAll()
            .Any(r => r.EmployeeId == employeeId && r.StartDate < incrementDate);

        if (!hasIncrement)
        {
            return result;
        }

        double provRate = 0;
        double prodDays = 0;
        double provAmount = 0;

        var salaryRegisterDaysList = new List<SalaryRegisterDayCount>();

        // Legacy 2463-2485.
        if (provisionLine != null)
        {
            provRate = provisionLine.Rate;
            prodDays = provisionLine.Number;
            provAmount = provisionLine.Amount;

            salaryRegisterDaysList.Add(new SalaryRegisterDayCount
            {
                Period = context.Register.ForPeriod,
                Rate = provisionLine.Rate,
                Number = provisionLine.Number,
                Amount = provisionLine.Amount
            });
        }

        // Legacy 2489.
        var provChangeAmount = await GetProvisionChangeAnnualLeaveAsync(employeeId, payEndDate, provRate, context.Register.Target);
        result.ProvisionChangeAmount = provChangeAmount;

        if (provChangeAmount == 0)
        {
            // Legacy 2490: everything below is inside `if (provChangeAmount != 0)`. See the class remarks for
            // the one deviation here (legacy would have committed an all-zero 3001 row when the line was
            // absent).
            return result;
        }

        // Legacy 2493-2501.
        var prevMonthEndDate = GetPreviousMonthEndDate(payDate);

        salaryRegisterDaysList.Add(new SalaryRegisterDayCount
        {
            Period = prevMonthEndDate,
            Rate = 0,
            Number = 0,
            Amount = provChangeAmount
        });

        // Legacy 2504-2516.
        provAmount += provChangeAmount;

        var saved = await _registerStore.SaveLineAsync(
            context.Register,
            new PayrollRegisterLine
            {
                Id = provisionLine != null ? provisionLine.Id : 0,
                WageTypeId = provisionLine != null ? provisionLine.WageTypeId : 0,
                WageCode = WageCodeAnnualLeaveProvision,
                Rate = provRate,
                Number = prodDays,
                Amount = provAmount
            },
            // Legacy calls Save() unconditionally here -- it does NOT apply the zero-means-delete rule on
            // this path (that already happened in the 3001 branch itself, legacy lines 283-288).
            deleteWhenAmountIsZero: false);

        result.PostedAmount = provAmount;
        result.SalaryRegisterDetailId = saved.Id;

        // Legacy 2519-2522: AddSalaryRegisterDaysDetail per row, ordered by Period. That legacy helper skips
        // rows where Rate, Number and Amount are all 0, which is reproduced by the Where below.
        var days = salaryRegisterDaysList
            .Where(d => d.Rate != 0 || d.Number != 0 || d.Amount != 0)
            .OrderBy(d => d.Period)
            .ToList();

        await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, days);

        result.CreatedDaysDetailCount = days.Count;
        return result;
    }

    #endregion

    #region << GetProvisionChangeAnnualLeave (legacy 2531) >>

    /// <inheritdoc/>
    public async Task<double> GetProvisionChangeAnnualLeaveAsync(int employeeId, DateTime payDate, double salaryInProvision, PayrollRegisterTarget registerTarget)
    {
        var prevMonthEndDate = GetPreviousMonthEndDate(payDate);

        // Legacy 2540-2548: previous month's REGULAR, non-retro register (ForPeriod == InPeriod == prev month
        // end). The commented-out fallback at legacy lines 2551-2560 is deliberately NOT ported.
        var prevSalReg = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.ForPeriod == prevMonthEndDate
                        && r.InPeriod == prevMonthEndDate
                        && r.PayrollRunType == PayrollRunType.Regular)
            .Select(r => new { r.Id, r.ForPeriod, r.InPeriod, r.PayrollRunType })
            .FirstOrDefault();

        if (prevSalReg == null)
        {
            return 0;
        }

        var prevRegisterRef = new PayrollRegisterRef
        {
            RegisterId = prevSalReg.Id,
            Target = registerTarget,
            ForPeriod = prevSalReg.ForPeriod,
            InPeriod = prevSalReg.InPeriod,
            PayrollRunType = prevSalReg.PayrollRunType
        };

        // Legacy 2582-2586.
        var prevALClosing = await _registerStore.GetLineAsync(prevRegisterRef, WageCodeTotalAnnualLeaveProvision);

        var prevALClosingBalance = prevALClosing != null ? prevALClosing.Number : 0;
        var prevALClosingAmountOldSalary = prevALClosing != null ? prevALClosing.Amount : 0;

        // Legacy 2589-2591: the divisor 26 and the 6 digits are literals in the legacy source.
        double alProvisionRateNewSalary = 0;
        if (salaryInProvision != 0)
        {
            alProvisionRateNewSalary = Math.Round(salaryInProvision / 26, 6);
        }

        var prevALClosingAmountNewSalary = prevALClosingBalance * alProvisionRateNewSalary;

        // Legacy 2595-2596.
        return prevALClosingAmountNewSalary - prevALClosingAmountOldSalary;
    }

    #endregion

    #region << Helpers >>

    /// <summary>
    /// Legacy <c>Program.AnnualLeaveOid</c>. Read from the run snapshot when the engine resolved it
    /// (<see cref="PayrollEngineSettings.AnnualLeaveAbsenceTypeId"/>, legacy Program.cs line 2710), falling
    /// back to the same <c>Code == "1"</c> lookup legacy's <c>CalculateAnnualLeaveProvision</c> does inline
    /// (legacy line 2381) when the snapshot has not been populated.
    /// </summary>
    private int ResolveAnnualLeaveAbsenceTypeId(PayrollCalculationContext context)
    {
        if (context.Settings != null && context.Settings.AnnualLeaveAbsenceTypeId != 0)
        {
            return context.Settings.AnnualLeaveAbsenceTypeId;
        }

        return _absenceTypeRepository.GetAll()
            .Where(a => a.Code == AbsenceTypeCodeAnnualLeave)
            .Select(a => a.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy 2290 / 2328 / 2382: wage types carrying a <see cref="WageTypeLeave"/> row for the annual-leave
    /// absence type. Legacy expressed it as <c>WageTypeList.Where(r =&gt; r.WageTypeLeave.Any(...))</c>; with
    /// no bidirectional collection in this port it is the equivalent query over <see cref="WageTypeLeave"/>.
    /// </summary>
    private List<int> GetPaidLeaveWageTypeIds(int annualLeaveAbsenceTypeId)
    {
        return _wageTypeLeaveRepository.GetAll()
            .Where(l => l.AbsenceTypeId == annualLeaveAbsenceTypeId)
            .Select(l => l.WageTypeId)
            .Distinct()
            .ToList();
    }

    /// <summary>Legacy's ubiquitous "previous month end" idiom (e.g. lines 2493-2494, 2536-2537).</summary>
    private static DateTime GetPreviousMonthEndDate(DateTime payDate)
    {
        var prev = payDate.AddMonths(-1);
        return new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month));
    }

    #endregion
}
