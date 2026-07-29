using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IEmployeeBasicPayCalculationService"/>
/// <remarks>
/// Ported from <c>HCM10.PayrollEngine/EmployeeBasicPayService.cs</c>, <c>Main</c> (line 16) and its private
/// <c>GetAmount</c> (line 105). <c>Main_Simu</c>/<c>GetAmount_Simu</c> (lines 1148-2256) are deliberately NOT
/// ported: wave P7 collapses the simulation clone by flipping
/// <see cref="PayrollRegisterRef.Target"/> on <see cref="IPayrollRegisterStore"/>.
///
/// <para><b>Deviations and preserved quirks -- all deliberate:</b></para>
/// <list type="number">
/// <item><description><b>Dependant branch writes <c>Number</c>, not <c>Amount</c>.</b> Legacy line 81 is
/// <c>salaryRegisterDetail.Number = amount;</c> after computing a percentage of the dependant wage type's
/// AMOUNT. Almost certainly a legacy defect, preserved verbatim -- changing it changes pay.</description></item>
/// <item><description><b>The line is always written, never "zero means delete".</b> Legacy ends both arms of
/// <c>Main</c> with an unconditional <c>salaryRegister.SalaryRegisterDetail.Add(...)</c> (lines 51, 94), so
/// <see cref="IPayrollRegisterStore.SaveLineAsync"/> is called with
/// <c>deleteWhenAmountIsZero: false</c>.</description></item>
/// <item><description><b><c>IsSystemGenerated</c>/<c>SysCreator</c>/<c>SysCreatedOn</c> not ported.</b>
/// <see cref="PayrollRegisterLine"/> is the target-agnostic mirror and carries no such fields (by design --
/// see <see cref="IPayrollRegisterStore"/>); ABP's <c>FullAuditedEntity</c> covers the audit half.
/// <b>Reported as a gap:</b> <c>SalaryRegisterDetail.IsSystemGenerated</c> is genuinely business data
/// (it distinguishes engine rows from manual ones) and no member of <see cref="PayrollRegisterLine"/>
/// carries it.</description></item>
/// <item><description><b>Dead MonthWiseInfo branch preserved.</b> Inside the retro-month arm
/// (<c>InPeriod != ForPeriod</c>) legacy still guards its month-wise write with
/// <c>salaryRegister.InPeriod == salaryRegister.ForPeriod</c> (legacy lines 478, 567, 608, 652) -- which is
/// false by construction there. The condition is reproduced literally, so the branch stays dead exactly as in
/// legacy rather than silently gaining rows.</description></item>
/// <item><description><b><c>Program.AdvancePaymentDed</c> (wage code 2005) is NOT ported and THROWS.</b> See
/// <see cref="AdvancePaymentDedAsync"/>.</description></item>
/// <item><description><b>No rounding anywhere.</b> Legacy's basic-pay body contains no <c>Math.Round</c> at
/// all -- rounding happens later, once, in <c>Program.SetRoundoff</c> (see
/// <see cref="IPayrollRoundingService"/>). <see cref="IPayrollRoundingService"/> is deliberately NOT injected
/// here.</description></item>
/// <item><description><b>No EF async.</b> <c>HCMv3.Core</c> has no EntityFrameworkCore reference, so every
/// query is plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>, as in
/// <c>AccrualService</c>/<c>PayrollDayCountService</c>.</description></item>
/// </list>
/// </remarks>
public class EmployeeBasicPayCalculationService : HCMv3DomainServiceBase, IEmployeeBasicPayCalculationService, ITransientDependency
{
    private const string BasicPayWageCode = "1000";
    private const string PublicHolidayDaysWageCode = "4007";
    private const string RestDaysWageCode = "4009";
    private const string ActiveDaysWageCode = "4011";
    private const string PresentDaysWageCode = "4014";

    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<EmployeeRemuneration> _employeeRemunerationRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollDayCountService _dayCountService;
    private readonly ITimeDataService _timeDataService;
    private readonly IRosterService _rosterService;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;

    public EmployeeBasicPayCalculationService(
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<EmployeeRemuneration> employeeRemunerationRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IPayrollRegisterStore registerStore,
        IPayrollDayCountService dayCountService,
        ITimeDataService timeDataService,
        IRosterService rosterService,
        IPayrollAdvancePaymentService advancePaymentService)
    {
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _employeeRemunerationRepository = employeeRemunerationRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _registerStore = registerStore;
        _dayCountService = dayCountService;
        _timeDataService = timeDataService;
        _rosterService = rosterService;
        _advancePaymentService = advancePaymentService;
    }

    #region << Legacy Program.cs lines 1918-1932 (wtBasicPayWageList) >>

    /// <inheritdoc/>
    public Task<List<int>> GetBasicPayWageTypeIdsAsync(int employeeId, DateTime payDate)
    {
        var details = GetBasicPayDetails(employeeId, payDate);
        return Task.FromResult(details.Select(r => r.WageTypeId).ToList());
    }

    /// <summary>
    /// Legacy <c>emp.BasicPay.Where(r =&gt; r.StartDate &lt;= payDate &amp;&amp; r.EndDate &gt;= payDate).FirstOrDefault()</c>
    /// then <c>.BasicPayDetail.ToList()</c> (legacy <c>Program.cs</c> lines 1918-1922). Legacy's
    /// <c>FirstOrDefault</c> over an unordered XPO collection is reproduced as an unordered
    /// <c>FirstOrDefault</c>; when more than one basic-pay row covers the date, both pick arbitrarily.
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

    #endregion

    #region << Main (legacy EmployeeBasicPayService.cs line 16) >>

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy line 24.
        var empBasicPayDetail = GetBasicPayDetails(context.EmployeeId, context.PayDate)
            .FirstOrDefault(r => r.WageTypeId == context.WageType.Id);

        if (empBasicPayDetail == null)
        {
            return;
        }

        var line = await GetOrCreateLineAsync(context);

        if (empBasicPayDetail.DependantWageTypeId == null)
        {
            // Legacy lines 28-52 -- "For Not Dependant".
            await GetAmountAsync(context, empBasicPayDetail, line);
        }
        else
        {
            // Legacy lines 56-95 -- "For Dependant".
            var allowed =
                (context.PayrollRunType == PayrollRunType.Regular && context.WageType.IncludeInBasicPay)
                || (context.PayrollRunType == PayrollRunType.OffCycle && context.WageType.IncludeInAdvancePay)
                || (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService);

            if (allowed)
            {
                var depLine = await _registerStore.GetLineByWageTypeIdAsync(
                    context.Register, empBasicPayDetail.DependantWageTypeId.Value);

                if (depLine != null)
                {
                    var amount = depLine.Amount;
                    amount = empBasicPayDetail.DependantUnit == DependantUnit.Percentage
                        ? (amount * empBasicPayDetail.DependantNumber) / 100
                        : 0;

                    // LEGACY QUIRK (line 81): assigns to Number, not Amount. Preserved verbatim.
                    line.Number = amount;
                }
            }
            else
            {
                line.Amount = 0;
            }
        }

        // Legacy adds the detail unconditionally (lines 51/94) -- no "zero means delete" here.
        await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);
    }

    /// <summary>Legacy lines 30-38 / 58-66.</summary>
    private async Task<PayrollRegisterLine> GetOrCreateLineAsync(PayrollCalculationContext context)
    {
        var line = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);

        return line ?? new PayrollRegisterLine
        {
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code
        };
    }

    #endregion

    #region << GetAmount (legacy EmployeeBasicPayService.cs line 105) >>

    private async Task GetAmountAsync(
        PayrollCalculationContext context,
        EmployeeBasicPayDetail empBasicPayDetail,
        PayrollRegisterLine line)
    {
        var payInterval = empBasicPayDetail.EmployeeBasicPay?.PayInterval
                          ?? GetPayIntervalOrThrow(empBasicPayDetail);

        if (payInterval == PayInterval.Monthly)
        {
            if (context.PayrollRunType == PayrollRunType.Regular)
            {
                await GetAmountMonthlyRegularAsync(context, empBasicPayDetail, line);
            }
            else if (context.PayrollRunType == PayrollRunType.OffCycle)
            {
                await GetAmountMonthlyOffCycleAsync(context, empBasicPayDetail, line);
            }
        }
        else if (payInterval == PayInterval.Daily)
        {
            await GetAmountDailyAsync(context, empBasicPayDetail, line);
        }
        else if (payInterval == PayInterval.Hourly)
        {
            await GetAmountHourlyAsync(context, empBasicPayDetail, line);
        }

        // Every other PayInterval (EndOfService, Yearly) falls through with no branch, exactly as legacy.
    }

    /// <summary>
    /// Legacy reads <c>empBasicPayDetail.EmployeeBasicPay.PayInterval</c> through a loaded XPO reference. When
    /// the navigation property is not populated, re-read it by FK rather than NullReference.
    /// </summary>
    private PayInterval GetPayIntervalOrThrow(EmployeeBasicPayDetail empBasicPayDetail)
    {
        var basicPay = _employeeBasicPayRepository.GetAll()
            .FirstOrDefault(r => r.Id == empBasicPayDetail.EmployeeBasicPayId);

        if (basicPay == null)
        {
            throw new InvalidOperationException(
                $"EmployeeBasicPay '{empBasicPayDetail.EmployeeBasicPayId}' not found for EmployeeBasicPayDetail '{empBasicPayDetail.Id}'.");
        }

        return basicPay.PayInterval;
    }

    #region << PayInterval = Monthly / Regular (legacy lines 116-362) >>

    private async Task GetAmountMonthlyRegularAsync(
        PayrollCalculationContext context,
        EmployeeBasicPayDetail empBasicPayDetail,
        PayrollRegisterLine line)
    {
        var payDate = context.PayDate;
        var wageType = context.WageType;
        var approvalOn = context.Settings.EnableTimeRegisterApproval;

        double payRate = empBasicPayDetail.Amount;
        double payWorkingdays = 0;
        double perDaySalary;

        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        {
            // Legacy lines 124-229.
            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            perDaySalary = payRate / calendarDays;

            var basicDetail = await _registerStore.GetLineAsync(context.Register, BasicPayWageCode);
            if (basicDetail != null && basicDetail.Rate != 0)
            {
                payWorkingdays = basicDetail.Number;
            }
            else
            {
                var rows = RegularRows(context, startDate, endDate);

                double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);
                double activeDays = await GetLineNumberAsync(context, ActiveDaysWageCode);
                double weekoffRestdayHoliday = 0;

                // PH without leave day (legacy lines 147-160).
                double phDays = await GetLineNumberAsync(context, PublicHolidayDaysWageCode);
                if (phDays != 0)
                {
                    var leaveOnPh = rows.Count(r => r.PublicHoliday == 1 && r.LeaveDay == 1);
                    weekoffRestdayHoliday += phDays - leaveOnPh;
                }

                // WOFF without leave day (legacy lines 162-179). NOTE legacy reads the SUM of the WeeklyOff
                // column here, NOT the 4008 register line (that read is commented out at legacy line 163).
                double woffDays = rows.Sum(r => r.WeeklyOff);
                if (woffDays != 0)
                {
                    var leaveOnWoff = rows.Count(r => r.WeeklyOff == 1
                                                      && (r.LeaveDay == 1
                                                          || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                    weekoffRestdayHoliday += woffDays - leaveOnWoff;
                }

                // REST without leave day (legacy lines 181-198). Same "column not 4009 line" note.
                double restDays = rows.Sum(r => r.RestDay);
                if (restDays != 0)
                {
                    var leaveOnRest = rows.Count(r => r.RestDay == 1
                                                      && (r.LeaveDay == 1
                                                          || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                    weekoffRestdayHoliday += restDays - leaveOnRest;
                }

                // IncludeInLeavePay (legacy lines 200-210).
                double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
                if (includeInLeavePay != 0)
                {
                    weekoffRestdayHoliday -= includeInLeavePay;
                }

                payWorkingdays = actualWorkingDays + weekoffRestdayHoliday;

                if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
                {
                    payWorkingdays = activeDays;
                }
            }

            line.Rate = payRate;
            if (!context.OnHold.BasicPay)
            {
                line.Number = payWorkingdays;
                line.Amount = perDaySalary * payWorkingdays;
            }
        }
        else if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            // Legacy lines 233-317.
            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            double weeklyOffDays = await GetActualWeekOffsAsync(context, payDate);
            double salaryBasisDays = calendarDays - weeklyOffDays;

            perDaySalary = payRate / salaryBasisDays;

            var basicDetail = await _registerStore.GetLineAsync(context.Register, BasicPayWageCode);
            if (basicDetail != null && basicDetail.Rate != 0)
            {
                payWorkingdays = basicDetail.Number;
            }
            else
            {
                var rows = RegularRows(context, startDate, endDate);

                double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);
                double phDays = await GetLineNumberAsync(context, PublicHolidayDaysWageCode);

                payWorkingdays = 0;

                if (wageType.ProrateMethod == ProrateMethod.WorkingDays)
                {
                    payWorkingdays = actualWorkingDays + phDays;
                }
                else if (wageType.ProrateMethod == ProrateMethod.WorkingDays_REST)
                {
                    double restDays = await GetLineNumberAsync(context, RestDaysWageCode);
                    payWorkingdays = actualWorkingDays + restDays;

                    // Legacy lines 275-283: subtract public holidays that fall on a DUTY weekly-off
                    // (deliberately Duty.IsWeeklyOff, NOT TimeRegister.WeeklyOff -- see legacy comment).
                    var publicHolidayOnWeekOff = rows.Count(r => r.PublicHoliday == 1
                                                                 && r.Duty != null
                                                                 && r.Duty.IsWeeklyOff);
                    payWorkingdays = payWorkingdays + (phDays - publicHolidayOnWeekOff);
                }
                else if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
                {
                    payWorkingdays = await GetLineNumberAsync(context, ActiveDaysWageCode);
                }

                double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
                if (includeInLeavePay != 0)
                {
                    payWorkingdays -= includeInLeavePay;
                }
            }

            line.Rate = payRate;
            if (!context.OnHold.BasicPay)
            {
                line.Number = payWorkingdays;
                line.Amount = perDaySalary * payWorkingdays;
            }
        }
        else if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
        {
            // Legacy lines 321-332.
            perDaySalary = payRate / wageType.FixedDays;
            payWorkingdays = await GetLineNumberAsync(context, PresentDaysWageCode);

            line.Rate = payRate;
            if (!context.OnHold.BasicPay)
            {
                line.Number = payWorkingdays;
                line.Amount = perDaySalary * payWorkingdays;
            }
        }
        else if (wageType.BillingMethod == BillingMethod.FixedAmountPerMonth)
        {
            // Legacy lines 336-351.
            var basicDetail = await _registerStore.GetLineAsync(context.Register, BasicPayWageCode);
            if (basicDetail != null && basicDetail.Rate != 0)
            {
                payWorkingdays = basicDetail.Number;
            }

            line.Rate = payRate;
            if (!context.OnHold.BasicPay)
            {
                line.Number = payWorkingdays;
                line.Amount = payRate;
            }
        }

        // Legacy lines 354-361.
        if (context.IsNotReturnFromLeave)
        {
            line.Number = 0;
            line.Amount = 0;
        }
    }

    #endregion

    #region << PayInterval = Monthly / OffCycle (legacy lines 366-1028) >>

    private async Task GetAmountMonthlyOffCycleAsync(
        PayrollCalculationContext context,
        EmployeeBasicPayDetail empBasicPayDetail,
        PayrollRegisterLine line)
    {
        // Legacy line 368.
        line.Rate = empBasicPayDetail.Amount;

        var allowed = (context.IsAdvanceLeave && context.WageType.IncludeInAdvancePay)
                      || context.IsEndOfService;

        if (!allowed)
        {
            // Legacy lines 1026-1027.
            line.Amount = 0;
            return;
        }

        var monthWiseDays = new List<SalaryRegisterDayCount>();

        if (context.Register.InPeriod != context.Register.ForPeriod)
        {
            await OffCycleRetroMonthAsync(context, line, monthWiseDays);
        }
        else
        {
            await OffCycleRecoveryPeriodsAsync(context, line, monthWiseDays);
        }

        if (monthWiseDays.Count > 0)
        {
            // The line must be persisted before day-detail rows can FK to it (InsertAndGetIdAsync semantics
            // live inside the store).
            var saved = await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);
            if (saved != null)
            {
                line.Id = saved.Id;
                await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, monthWiseDays);
            }
        }
    }

    /// <summary>Legacy lines 374-660 -- "For Retro Month" (<c>InPeriod != ForPeriod</c>).</summary>
    private async Task OffCycleRetroMonthAsync(
        PayrollCalculationContext context,
        PayrollRegisterLine line,
        List<SalaryRegisterDayCount> monthWiseDays)
    {
        var payDate = context.PayDate;
        var wageType = context.WageType;
        var approvalOn = context.Settings.EnableTimeRegisterApproval;

        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = payDate;

        double workingDays;
        double amountpay;

        if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        {
            // Legacy lines 377-484.
            double calendarDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            double perDaySalary = line.Rate / calendarDays;

            var rows = OffCycleRows(context, startDate, endDate);
            double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);

            double weekoffRestdayHoliday = 0;

            double phDays = await _dayCountService.GetPublicHolidayDaysAsync(
                context.EmployeeId, endDate, context.PayrollRunType, endDate);
            if (phDays != 0)
            {
                var leaveOnPh = rows.Count(r => r.PublicHoliday == 1 && r.LeaveDay == 1);
                weekoffRestdayHoliday += phDays - leaveOnPh;
            }

            double woffDays = rows.Sum(r => r.WeeklyOff);
            if (woffDays != 0)
            {
                var leaveOnWoff = rows.Count(r => r.WeeklyOff == 1
                                                  && (r.LeaveDay == 1
                                                      || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                weekoffRestdayHoliday += woffDays - leaveOnWoff;
            }

            double restDays = await _dayCountService.GetRestDaysAsync(
                context.EmployeeId, endDate, context.PayrollRunType, endDate);
            if (restDays != 0)
            {
                var leaveOnRest = rows.Count(r => r.RestDay == 1
                                                  && (r.LeaveDay == 1
                                                      || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                weekoffRestdayHoliday += restDays - leaveOnRest;
            }

            double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
            if (includeInLeavePay != 0)
            {
                weekoffRestdayHoliday -= includeInLeavePay;
            }

            workingDays = actualWorkingDays + weekoffRestdayHoliday;

            if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
            {
                workingDays = await _dayCountService.GetActiveDaysAsync(
                    context.EmployeeId, startDate, endDate, context.PayrollRunType, endDate);
            }

            amountpay = perDaySalary * workingDays;

            line.Number += workingDays;
            line.Amount += amountpay;

            if (context.IsNotReturnFromLeave)
            {
                line.Number = 0;
                line.Amount = 0;
                amountpay = 0;
            }

            AddMonthWiseInfo(context, line, monthWiseDays, payDate, workingDays, amountpay);
        }
        else if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            // Legacy lines 488-573.
            double calendarDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            double phDays = await _dayCountService.GetPublicHolidayDaysAsync(
                context.EmployeeId, endDate, context.PayrollRunType, endDate);
            double weeklyOffDays = await GetActualWeekOffsAsync(context, startDate);

            double salaryBasisDays = calendarDays - weeklyOffDays;
            double perDaySalary = line.Rate / salaryBasisDays;

            var rows = OffCycleRows(context, startDate, endDate);
            double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);

            workingDays = 0;

            if (wageType.ProrateMethod == ProrateMethod.WorkingDays)
            {
                workingDays = actualWorkingDays + phDays;
            }
            else if (wageType.ProrateMethod == ProrateMethod.WorkingDays_REST)
            {
                double restDays = await _dayCountService.GetRestDaysAsync(
                    context.EmployeeId, endDate, context.PayrollRunType, endDate);
                workingDays = actualWorkingDays + restDays;
                workingDays += phDays;
            }
            else if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
            {
                workingDays = await _dayCountService.GetActiveDaysAsync(
                    context.EmployeeId, startDate, endDate, context.PayrollRunType, endDate);
            }

            double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
            if (includeInLeavePay != 0)
            {
                workingDays -= includeInLeavePay;
            }

            amountpay = perDaySalary * workingDays;

            line.Number += workingDays;
            line.Amount += amountpay;

            if (context.IsNotReturnFromLeave)
            {
                line.Number = 0;
                line.Amount = 0;
                amountpay = 0;
            }

            AddMonthWiseInfo(context, line, monthWiseDays, payDate, workingDays, amountpay);
        }
        else if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
        {
            // Legacy lines 577-614.
            var presentDays = await _dayCountService.GetPresentDaysAsync(
                context.EmployeeId, startDate, endDate, context.Register.RegisterId,
                context.PayrollRunType, context.OffCycleEndDate, context.OffCycleReason);

            workingDays = presentDays.PresentDays;

            double salaryBasisDays = wageType.FixedDays;
            double perDaySalary = line.Rate / salaryBasisDays;
            amountpay = perDaySalary * workingDays;

            line.Number += workingDays;
            line.Amount += amountpay;

            if (context.IsNotReturnFromLeave)
            {
                line.Number = 0;
                line.Amount = 0;
                amountpay = 0;
            }

            AddMonthWiseInfo(context, line, monthWiseDays, payDate, workingDays, amountpay);
        }
        else if (wageType.BillingMethod == BillingMethod.FixedAmountPerMonth)
        {
            // Legacy lines 618-658.
            var monthEnddate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));
            double payRate = line.Rate;
            double payWorkingdays = await GetLineNumberAsync(context, BasicPayWageCode);
            amountpay = payRate;

            if (context.IsEndOfService)
            {
                var separation = GetUnrevokedSeparation(context.EmployeeId);
                if (separation != null)
                {
                    var allowUptoDate = MonthEnd(separation.LastDayInService);
                    if (allowUptoDate < monthEnddate)
                    {
                        amountpay = 0;
                    }
                }
            }

            line.Rate = payRate;
            line.Number = payWorkingdays;
            line.Amount = amountpay;

            if (context.IsNotReturnFromLeave)
            {
                line.Number = 0;
                line.Amount = 0;
                amountpay = 0;
            }

            AddMonthWiseInfo(context, line, monthWiseDays, payDate, payWorkingdays, amountpay);
        }
    }

    /// <summary>Legacy lines 664-1023 -- "Get OffCycle Month" (<c>InPeriod == ForPeriod</c>).</summary>
    private async Task OffCycleRecoveryPeriodsAsync(
        PayrollCalculationContext context,
        PayrollRegisterLine line,
        List<SalaryRegisterDayCount> monthWiseDays)
    {
        var payDate = context.PayDate;
        var wageType = context.WageType;
        var approvalOn = context.Settings.EnableTimeRegisterApproval;

        var recoveryPeriods = GetRecoveryPeriods(payDate, context.OffCycleEndDate);

        foreach (var rec in recoveryPeriods)
        {
            var monthEnddate = new DateTime(rec.Year, rec.Month, DateTime.DaysInMonth(rec.Year, rec.Month));
            if (monthEnddate > context.OffCycleEndDate)
            {
                monthEnddate = context.OffCycleEndDate;
            }

            var startDate = rec;
            var endDate = monthEnddate;

            double workingDays;
            double amountpay;

            if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
            {
                // Legacy lines 671-796.
                double calendarDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                double perDaySalary = line.Rate / calendarDays;

                var rows = OffCycleRows(context, startDate, endDate);
                double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);

                double weekoffRestdayHoliday = 0;

                double phDays = await _dayCountService.GetPublicHolidayDaysAsync(
                    context.EmployeeId, endDate, context.PayrollRunType, endDate);
                if (phDays != 0)
                {
                    var leaveOnPh = rows.Count(r => r.PublicHoliday == 1 && r.LeaveDay == 1);
                    weekoffRestdayHoliday += phDays - leaveOnPh;
                }

                double woffDays = rows.Sum(r => r.WeeklyOff);
                if (woffDays != 0)
                {
                    var leaveOnWoff = rows.Count(r => r.WeeklyOff == 1
                                                      && (r.LeaveDay == 1
                                                          || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                    weekoffRestdayHoliday += woffDays - leaveOnWoff;
                }

                double restDays = await _dayCountService.GetRestDaysAsync(
                    context.EmployeeId, endDate, context.PayrollRunType, endDate);
                if (restDays != 0)
                {
                    var leaveOnRest = rows.Count(r => r.RestDay == 1
                                                      && (r.LeaveDay == 1
                                                          || (approvalOn ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1)));
                    weekoffRestdayHoliday += restDays - leaveOnRest;
                }

                double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
                if (includeInLeavePay != 0)
                {
                    weekoffRestdayHoliday -= includeInLeavePay;
                }

                workingDays = actualWorkingDays + weekoffRestdayHoliday;

                if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
                {
                    workingDays = await _dayCountService.GetActiveDaysAsync(
                        context.EmployeeId, startDate, endDate, context.PayrollRunType, endDate);
                }

                amountpay = perDaySalary * workingDays;
            }
            else if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
            {
                // Legacy lines 800-901.
                double calendarDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                double phDays = await _dayCountService.GetPublicHolidayDaysAsync(
                    context.EmployeeId, endDate, context.PayrollRunType, endDate);
                double weeklyOffDays = await GetActualWeekOffsAsync(context, startDate);

                double salaryBasisDays = calendarDays - weeklyOffDays;
                double perDaySalary = line.Rate / salaryBasisDays;

                var rows = OffCycleRows(context, startDate, endDate);
                double actualWorkingDays = rows.Sum(r => approvalOn ? r.ApprovedActualWorkingDay : r.ActualWorkingDay);

                workingDays = 0;

                if (wageType.ProrateMethod == ProrateMethod.WorkingDays)
                {
                    workingDays = actualWorkingDays + phDays;
                }
                else if (wageType.ProrateMethod == ProrateMethod.WorkingDays_REST)
                {
                    double restDays = await _dayCountService.GetRestDaysAsync(
                        context.EmployeeId, endDate, context.PayrollRunType, endDate);
                    workingDays = actualWorkingDays + restDays;
                    workingDays += phDays;
                }
                else if (wageType.ProrateMethod == ProrateMethod.ActiveDays)
                {
                    workingDays = await _dayCountService.GetActiveDaysAsync(
                        context.EmployeeId, startDate, endDate, context.PayrollRunType, endDate);
                }

                double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
                if (includeInLeavePay != 0)
                {
                    workingDays -= includeInLeavePay;
                }

                amountpay = perDaySalary * workingDays;
            }
            else if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
            {
                // Legacy lines 905-959.
                var presentDays = await _dayCountService.GetPresentDaysAsync(
                    context.EmployeeId, rec, monthEnddate, context.Register.RegisterId,
                    context.PayrollRunType, context.OffCycleEndDate, context.OffCycleReason);

                workingDays = presentDays.PresentDays;

                double salaryBasisDays = wageType.FixedDays;
                double perDaySalary = line.Rate / salaryBasisDays;
                amountpay = perDaySalary * workingDays;
            }
            else if (wageType.BillingMethod == BillingMethod.FixedAmountPerMonth)
            {
                // Legacy lines 963-1021.
                double payRate = line.Rate;
                workingDays = await GetLineNumberAsync(context, BasicPayWageCode);
                amountpay = payRate;

                if (context.IsEndOfService)
                {
                    var separation = GetUnrevokedSeparation(context.EmployeeId);
                    if (separation != null)
                    {
                        var allowUptoDate = MonthEnd(separation.LastDayInService);
                        if (allowUptoDate < monthEnddate)
                        {
                            amountpay = 0;
                        }
                    }
                }

                // LEGACY ASYMMETRY (line 992): Number is ASSIGNED here, while every other branch ACCUMULATES
                // with +=. Preserved verbatim.
                line.Number = workingDays;
                line.Amount += amountpay;

                if (context.IsNotReturnFromLeave)
                {
                    line.Number = 0;
                    line.Amount = 0;
                    amountpay = 0;
                }

                await AdvancePaymentDedAsync(context, rec, amountpay);
                AddMonthWiseInfo(context, line, monthWiseDays, rec, workingDays, amountpay);
                continue;
            }
            else
            {
                continue;
            }

            line.Number += workingDays;
            line.Amount += amountpay;

            if (context.IsNotReturnFromLeave)
            {
                line.Number = 0;
                line.Amount = 0;
                amountpay = 0;
            }

            await AdvancePaymentDedAsync(context, rec, amountpay);
            AddMonthWiseInfo(context, line, monthWiseDays, rec, workingDays, amountpay);
        }
    }

    #endregion

    #region << PayInterval = Daily (legacy lines 1035-1083) >>

    private async Task GetAmountDailyAsync(
        PayrollCalculationContext context,
        EmployeeBasicPayDetail empBasicPayDetail,
        PayrollRegisterLine line)
    {
        var payDate = context.PayDate;
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        var empRemunerationList = _employeeRemunerationRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.PaymentDate >= startDate && r.PaymentDate <= endDate)
            .ToList();

        double totalHoursInDay = await _timeDataService.GetTotalHoursInDayAsync(context.Employee, payDate);
        totalHoursInDay = totalHoursInDay != 0 ? totalHoursInDay : context.Settings.DefaultWorkingHours;

        double payRate = empBasicPayDetail.Amount;
        payRate = await ScalePayRateByBillingMethodAsync(context, payRate);

        double perDaySalary = empBasicPayDetail.Amount;
        double payWorkingdays = SumRemunerationDays(empRemunerationList, totalHoursInDay);

        line.Rate = payRate;
        if (!context.OnHold.BasicPay)
        {
            line.Number = payWorkingdays;
            line.Amount = perDaySalary * payWorkingdays;
        }
    }

    #endregion

    #region << PayInterval = Hourly (legacy lines 1088-1137) >>

    private async Task GetAmountHourlyAsync(
        PayrollCalculationContext context,
        EmployeeBasicPayDetail empBasicPayDetail,
        PayrollRegisterLine line)
    {
        var payDate = context.PayDate;
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        var empRemunerationList = _employeeRemunerationRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.PaymentDate >= startDate && r.PaymentDate <= endDate)
            .ToList();

        double totalHoursInDay = await _timeDataService.GetTotalHoursInDayAsync(context.Employee, payDate);
        totalHoursInDay = totalHoursInDay != 0 ? totalHoursInDay : context.Settings.DefaultWorkingHours;

        double payRate = empBasicPayDetail.Amount * totalHoursInDay;
        payRate = await ScalePayRateByBillingMethodAsync(context, payRate);

        double perDaySalary = empBasicPayDetail.Amount * totalHoursInDay;
        double payWorkingdays = SumRemunerationDays(empRemunerationList, totalHoursInDay);

        line.Rate = payRate;
        if (!context.OnHold.BasicPay)
        {
            line.Number = payWorkingdays;
            line.Amount = perDaySalary * payWorkingdays;
        }
    }

    #endregion

    /// <summary>Legacy "Calculate payRate" regions, identical in the Daily (lines 1045-1064) and Hourly (lines 1100-1118) arms.</summary>
    private async Task<double> ScalePayRateByBillingMethodAsync(PayrollCalculationContext context, double payRate)
    {
        var payDate = context.PayDate;
        var wageType = context.WageType;

        if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        {
            return payRate * DateTime.DaysInMonth(payDate.Year, payDate.Month);
        }

        if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            double weeklyOffDays = await GetActualWeekOffsAsync(context, payDate);
            return payRate * (calendarDays - weeklyOffDays);
        }

        if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
        {
            return payRate * wageType.FixedDays;
        }

        // FixedAmountPerMonth: legacy's "payRate = payRate" no-op.
        return payRate;
    }

    /// <summary>Legacy lines 1069-1075 / 1123-1129.</summary>
    private static double SumRemunerationDays(List<EmployeeRemuneration> empRemunerationList, double totalHoursInDay)
    {
        double payWorkingdays = 0;

        foreach (var empRemuneration in empRemunerationList)
        {
            if (empRemuneration.PaymentUnit == PaymentUnit.Days)
            {
                payWorkingdays += empRemuneration.PaymentNumber;
            }
            else if (empRemuneration.PaymentUnit == PaymentUnit.Hours && totalHoursInDay != 0)
            {
                payWorkingdays += empRemuneration.PaymentNumber / totalHoursInDay;
            }
        }

        return payWorkingdays;
    }

    #endregion

    #region << Helpers ported from Program.cs >>

    /// <summary>
    /// Legacy <c>empTimeRegisterList</c> reads (the REGULAR-run cache, legacy <c>Program.cs</c> lines
    /// 1614-1616). Legacy nulls this list on off-cycle runs; the regular arm is the only caller, so the
    /// null-check here is defensive only.
    /// </summary>
    private static List<TimeRegister> RegularRows(PayrollCalculationContext context, DateTime startDate, DateTime endDate)
    {
        if (context.TimeRegisterList == null)
        {
            return new List<TimeRegister>();
        }

        return context.TimeRegisterList
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .ToList();
    }

    /// <summary>
    /// Legacy <c>empTimeRegister</c> reads (the UNFILTERED IQueryable -- see
    /// <see cref="PayrollCalculationContext.TimeRegisterQuery"/> remarks; every legacy consumer re-applies its
    /// own employee/date predicate, which is exactly what happens here).
    /// </summary>
    private List<TimeRegister> OffCycleRows(PayrollCalculationContext context, DateTime startDate, DateTime endDate)
    {
        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();

        return query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .ToList();
    }

    private async Task<double> GetLineNumberAsync(PayrollCalculationContext context, string wageCode)
    {
        var line = await _registerStore.GetLineAsync(context.Register, wageCode);
        return line?.Number ?? 0;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetActualWeekOffs</c> (legacy <c>Program.cs</c> line 2858) -- the
    /// payroll-engine variant, NOT the XAF <c>AccrualService.GetActualWeekOffs</c>
    /// (<see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>), which differs in window,
    /// back-fill, counting rule and failure mode (see <see cref="IPayrollDayCountService.GetWeeklyOffDaysAsync"/>
    /// remarks).
    ///
    /// <para><b>Why it is duplicated here.</b> <c>PayrollDayCountService</c> already contains a private
    /// <c>GetPayrollActualWeekOffsAsync</c> with this exact body, but it is not exposed on
    /// <see cref="IPayrollDayCountService"/> and that file is frozen for this wave. <b>Reported:</b> promote it
    /// to the interface and delete this copy.</para>
    /// </summary>
    private async Task<double> GetActualWeekOffsAsync(PayrollCalculationContext context, DateTime payDate)
    {
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));
        double daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();
        var daysWithTimeRegister = query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => r.DayDate)
            .ToList();

        // Legacy line 2871 passes EndDate as BOTH payDate and offCycleEndDate.
        var weeklyOffDays = await _dayCountService.GetWeeklyOffDaysAsync(
            context.EmployeeId, endDate, context.PayrollRunType, endDate, context.OffCycleReason);

        if (Math.Abs(daysInMonth - daysWithTimeRegister.Count) > double.Epsilon)
        {
            List<EmployeeDutyPeriod> employeeDutyPeriods = null;

            for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
            {
                if (daysWithTimeRegister.Contains(dayDate))
                {
                    continue;
                }

                if (context.Settings.IsRosterActive)
                {
                    var rosterDuty = await _rosterService.GetRosterDutyAsync(context.EmployeeId, dayDate, null, false);
                    weeklyOffDays += rosterDuty != null && rosterDuty.IsWeeklyOff ? 1 : 0;
                    continue;
                }

                employeeDutyPeriods ??= _employeeDutyPeriodRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId)
                    .ToList();

                var dutyPeriod = employeeDutyPeriods
                    .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                    .Select(r => r.DutyPeriod)
                    .FirstOrDefault();

                if (dutyPeriod == null)
                {
                    throw new InvalidOperationException(
                        $"DutyPeriod not Found for Employee Id '{context.EmployeeId}' For '{dayDate:dd/MM/yyyy}'");
                }

                var duty = GetDutyForDayOfWeek(dutyPeriod, dayDate);
                weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
            }
        }

        return weeklyOffDays;
    }

    private static Duty GetDutyForDayOfWeek(DutyPeriod dutyPeriod, DateTime dayDate)
    {
        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDuty,
            DayOfWeek.Monday => dutyPeriod.MondayDuty,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDuty,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDuty,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDuty,
            DayOfWeek.Friday => dutyPeriod.FridayDuty,
            _ => dutyPeriod.SaturdayDuty
        };
    }

    /// <summary>Ported verbatim from legacy <c>Program.GetRecoveryPeriods</c> (legacy <c>Program.cs</c> line 2922).</summary>
    public static List<DateTime> GetRecoveryPeriods(DateTime payDate, DateTime offCycleEndDate)
    {
        var recoveryPeriods = new List<DateTime>();
        var recoveryStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        while (recoveryStartDate <= offCycleEndDate)
        {
            var monthStart = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, 1, 0, 0, 0);
            if (!recoveryPeriods.Contains(monthStart))
            {
                recoveryPeriods.Add(monthStart);
            }

            var daysInMonth = DateTime.DaysInMonth(recoveryStartDate.Year, recoveryStartDate.Month);
            recoveryStartDate = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, daysInMonth, 0, 0, 0).AddDays(1);
        }

        return recoveryPeriods;
    }

    /// <summary>
    /// Legacy <c>Program.AddSalaryRegisterDaysDetail</c> (legacy <c>Program.cs</c> line 5439) plus its
    /// caller-side guard (legacy <c>EmployeeBasicPayService.cs</c> lines 478, 567, 608, 652, 789, 894, 952, 1014).
    ///
    /// <para>Legacy's <c>Program.calculateSalRegDaysDetail</c> gate is a hardcoded <c>true</c> literal, never
    /// assigned -- see <see cref="PresentDaysResult.MonthWiseDays"/> remarks -- so it is not carried.</para>
    ///
    /// <para>Legacy's <c>rate != 0 || number != 0 || amount != 0</c> row-level guard (line 5443) is applied
    /// here too. Legacy swallows any exception from this method (line 5464); no exception is possible in this
    /// port's in-memory accumulation, so nothing is swallowed.</para>
    /// </summary>
    private static void AddMonthWiseInfo(
        PayrollCalculationContext context,
        PayrollRegisterLine line,
        List<SalaryRegisterDayCount> monthWiseDays,
        DateTime periodSeed,
        double number,
        double amount)
    {
        if (context.PayrollRunType != PayrollRunType.OffCycle
            || context.Register.InPeriod != context.Register.ForPeriod
            || amount == 0)
        {
            return;
        }

        var period = MonthEnd(periodSeed);

        if (line.Rate == 0 && number == 0 && amount == 0)
        {
            return;
        }

        monthWiseDays.Add(new SalaryRegisterDayCount
        {
            Period = period,
            Rate = line.Rate,
            Number = number,
            Amount = amount
        });
    }

    /// <summary>
    /// Legacy <c>emp.Separation.Where(r =&gt; r.RevokeDate == default(DateTime)).FirstOrDefault()</c> (legacy
    /// <c>EmployeeBasicPayService.cs</c> lines 628, 983). This port's <c>EmployeeSeparation.RevokeDate</c> is
    /// <c>DateTime?</c>, so "not revoked" is <c>null</c> -- same convention
    /// <see cref="WageTypeCalculatorResolver"/> uses.
    /// </summary>
    private EmployeeSeparation GetUnrevokedSeparation(int employeeId)
    {
        return _employeeSeparationRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employeeId && r.RevokeDate == null);
    }

    private static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    /// <summary>
    /// Legacy <c>Program.AdvancePaymentDed</c> (legacy <c>Program.cs</c> line 2940): upserts the employee's
    /// wage-code-<see cref="PayrollEngineWageCodes.AdvancePaymentDeduction"/> ("2005")
    /// <see cref="EmployeePayment"/> row, keyed by
    /// <c>(Employee, PaymentDate, OffCyclePaymentDate, WageType)</c>.
    ///
    /// <para>WAVE P8: previously threw, because this port's <see cref="EmployeePayment"/> had dropped
    /// <c>OffCyclePaymentDate</c> and matching without it merges recovery rows across off-cycle runs and
    /// over-deducts. That column (and <c>IsSystemGenerated</c>) is now on the entity, so this simply
    /// delegates to the single shared implementation in <see cref="IPayrollAdvancePaymentService"/> --
    /// the same one the ~12 wage-type calculators use -- instead of duplicating it. Legacy's two early
    /// exits (<c>amount == 0</c>, off-cycle End-Of-Service) live there and are kept here too so the
    /// behaviour is unchanged for the paths legacy no-ops on.</para>
    /// </summary>
    private Task AdvancePaymentDedAsync(PayrollCalculationContext context, DateTime paymentDate, double amount)
    {
        // Legacy line 2942.
        if (amount == 0)
        {
            return Task.CompletedTask;
        }

        // Legacy lines 2944-2947.
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

    #endregion
}
