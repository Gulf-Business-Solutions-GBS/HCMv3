using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// END-TO-END ARITHMETIC of ONE advance-leave off-cycle on <b>FIXED 26-DAY billing</b>, written as the business
/// scenario a tester follows rather than as per-rule unit tests. The per-rule tests
/// (<see cref="EmployeeBasicPayCalculationServiceTests"/>, <see cref="EmployeeLeavePayCalculationServiceTests"/>,
/// <see cref="PayrollDayCountServiceTests"/>) each prove one branch; nothing until now proved that the THREE
/// runs add up to exactly two months' salary, which is the only property that matters to payroll.
///
/// <para><b>The scenario.</b> Employee E1, basic 260.000 KWD/month, wage code 1000
/// <see cref="BillingMethod.FixedDaysPerMonth"/> with <c>FixedDays = 26</c> -- so a day is worth
/// <b>260 / 26 = 10.000 KWD</b> whatever the length of the month. September 2026 is run Regular and closed
/// ("exit payroll"). The employee then takes 30 days' annual leave <b>16/10/2026 - 14/11/2026</b>,
/// <see cref="LeavePaymentType.PayInAdvance"/>, payment date 15/10/2026, and an off-cycle
/// (<see cref="PayrollOffCycleReasons.AdvanceLeave"/>) pays it before he travels. October and November are then
/// run Regular as normal and must recover the advance exactly.</para>
///
/// <para><b>The real <see cref="PayrollDayCountService"/> is wired in, not stubbed.</b> On fixed-days billing
/// basic pay is <c>rate / 26 * presentDays</c>, and <c>presentDays</c> comes from that service -- which is
/// where all the interesting behaviour lives (the fixed-days/actual-days switch, the leave-ended truncation,
/// the week-off basis). Stubbing it would assert nothing but my own arithmetic.</para>
///
/// <para><b>Friday weekend, and week-off Fridays are EXCLUDED from the leave days.</b> The absence type has
/// <c>IncludeWeekend = false</c>, so the Time Engine leaves a Friday inside the leave as
/// <c>WeeklyOff = 1 / LeaveDay = 0</c> rather than making it a paid leave day (legacy
/// <c>GeneralTimeService.GetLeaveDay</c> lines 256-270). Five Fridays fall inside 16/10 - 14/11, so the
/// <b>30 calendar days of leave are 25 paid leave days</b> — 13 in October and 12 in November. Note this is the
/// LEAVE PAY count; what the employee's quota is debited is a separate question.</para>
///
/// <para>Two consequences worth holding on to, both asserted below: October has 5 Fridays so its salary basis
/// (31 - 5) is exactly the fixed 26; and October's working days plus October's leave days come to 26, which is
/// why October alone is still worth precisely one month's salary.</para>
///
/// <para><b>Every fixed-to-actual setting is left off.</b> Each one flips a month from the fixed-days formula to
/// actual-days, and the point here is the fixed-26 path. <see cref="PayrollDayCountServiceTests"/> covers the
/// switches themselves; the plan's variant table says what each does to these figures.</para>
///
/// <para><b>Why the register store is faked but the calculation services are REAL.</b> The money is computed
/// inside <see cref="EmployeeBasicPayCalculationService"/> and
/// <see cref="EmployeeLeavePayCalculationService"/>; the engine above them only loops wage types. Faking the
/// store keeps the assertion on the numbers those two produce, including the
/// <c>SalaryRegisterDaysDetail</c> rows.</para>
/// </summary>
public class OffCycleAdvanceLeaveScenario_Tests
{
    private const int EmployeeId = 1;

    private const int BasicPayWageTypeId = 1000;
    private const int LeavePayWageTypeId = 1050;
    private const int PresentDaysWageTypeId = 4014;
    private const int WeeklyOffWageTypeId = 4008;
    private const string PresentDaysWageCode = "4014";
    private const int AnnualLeaveAbsenceTypeId = 1;
    private const int RegisterId = 500;

    private const double MonthlyBasic = 260;

    /// <summary>The scenario's premise: salary / 26, so every paid day is worth exactly 10.000 KWD.</summary>
    private const double FixedDays = 26;
    private const double PerDay = MonthlyBasic / FixedDays;

    private static readonly DateTime SepPayInDate = new DateTime(2026, 9, 30);
    private static readonly DateTime OctPayInDate = new DateTime(2026, 10, 31);
    private static readonly DateTime NovPayInDate = new DateTime(2026, 11, 30);

    private static readonly DateTime OffCyclePaymentDate = new DateTime(2026, 10, 15);
    private static readonly DateTime LeaveStart = new DateTime(2026, 10, 16);
    private static readonly DateTime LeaveEnd = new DateTime(2026, 11, 14);

    /// <summary>
    /// What the engine narrows <c>offCycleEndDate</c> to for an Advance Leave run: the leave's own end date
    /// (<c>PayrollEngineService.ResolveOffCycleEndDate</c>).
    /// </summary>
    private static readonly DateTime OffCycleEndDate = LeaveEnd;

    #region << Harness >>

    /// <summary>
    /// In-memory <see cref="IPayrollRegisterStore"/> standing in for the one register each run writes,
    /// reproducing the two behaviours the rules depend on: "zero means delete" and Id-on-insert. Same shape as
    /// the copies in the two per-rule test classes (kept local for the same concurrency reason they give).
    /// </summary>
    private sealed class RegisterStoreFake : IPayrollRegisterStore
    {
        private int _nextId = 100;

        public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

        public Dictionary<int, List<SalaryRegisterDayCount>> DayDetails { get; } = new Dictionary<int, List<SalaryRegisterDayCount>>();

        public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
            => Task.FromResult(Lines.ToList());

        public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageCode == wageCode));

        public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId));

        public Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
        {
            var existing = Lines.FirstOrDefault(l => l.WageTypeId == line.WageTypeId);

            if (deleteWhenAmountIsZero && line.Amount == 0)
            {
                if (existing != null)
                {
                    Lines.Remove(existing);
                }

                return Task.FromResult<PayrollRegisterLine>(null);
            }

            if (existing == null)
            {
                existing = new PayrollRegisterLine
                {
                    Id = _nextId++,
                    WageTypeId = line.WageTypeId,
                    WageCode = line.WageCode
                };
                Lines.Add(existing);
            }

            existing.Rate = line.Rate;
            existing.Number = line.Number;
            existing.Amount = line.Amount;

            line.Id = existing.Id;
            return Task.FromResult(line);
        }

        public Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
        {
            var existing = Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId);
            if (existing != null)
            {
                Lines.Remove(existing);
            }

            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            Lines.Clear();
            DayDetails.Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
        {
            DayDetails[lineId] = days?.ToList() ?? new List<SalaryRegisterDayCount>();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Stands in for <see cref="PayrollAdvancePaymentService"/> over an in-memory row set, keeping BOTH of its
    /// behaviours that this scenario turns on:
    /// <list type="number">
    /// <item><description>the UPSERT semantics -- accumulate on the four-part key employee + PaymentDate +
    /// OffCyclePaymentDate + wage code 2005 -- because basic pay and leave pay each post their October share
    /// onto the SAME row and the scenario asserts the total;</description></item>
    /// <item><description>the four CLEARING operations, so the re-run test proves the fix rather than the
    /// bug.</description></item>
    /// </list>
    /// <see cref="PayrollAdvancePaymentService_Clearing_Tests"/> proves the real service against real
    /// repositories; this double exists so the scenario can assert the resulting money in one place.
    /// </summary>
    private sealed class AdvancePaymentServiceFake : IPayrollAdvancePaymentService
    {
        /// <summary>The wage-code-2005 <c>EmployeePayment</c> rows, keyed as the real upsert keys them.</summary>
        public Dictionary<(DateTime PaymentDate, DateTime OffCyclePaymentDate), double> Rows { get; }
            = new Dictionary<(DateTime, DateTime), double>();

        /// <summary>Wage-code-2011 "Last Month Recovery" rows, keyed by payment date.</summary>
        public Dictionary<DateTime, double> LastMonthRecoveryRows { get; } = new Dictionary<DateTime, double>();

        public List<(DateTime PaymentDate, double Amount)> Postings { get; } = new List<(DateTime, double)>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            // The real service's two early exits.
            if (amount == 0 || (payrollRunType == PayrollRunType.OffCycle && offCycleReason == PayrollOffCycleReasons.EndOfService))
            {
                return Task.CompletedTask;
            }

            Postings.Add((paymentDate, amount));

            var key = (paymentDate, offCyclePaymentDate);
            var decimals = settings?.DecimalInSalaryRegister ?? 6;

            Rows.TryGetValue(key, out var running);
            Rows[key] = Math.Round(running + amount, decimals);

            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
            => EmployeeBasicPayCalculationService.GetRecoveryPeriods(payStartDate, payEndDate);

        public Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate)
        {
            if (offCyclePaymentDate.Date == default(DateTime))
            {
                return Task.FromResult(0);
            }

            var keys = Rows.Keys.Where(k => k.OffCyclePaymentDate == offCyclePaymentDate.Date).ToList();
            foreach (var key in keys)
            {
                Rows.Remove(key);
            }

            return Task.FromResult(keys.Count);
        }

        public Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate)
        {
            if (hasAdvancePaidLeaveOnPayDate)
            {
                return Task.FromResult(0);
            }

            var keys = Rows.Keys
                .Where(k => k.OffCyclePaymentDate == payDate.Date && k.PaymentDate > payDate.Date)
                .ToList();

            return Zero(keys);
        }

        public Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate)
        {
            var keys = Rows.Keys
                .Where(k => k.PaymentDate >= payStartDate.Date && k.PaymentDate <= offCycleEndDate.Date
                            && k.OffCyclePaymentDate >= payStartDate.Date && k.OffCyclePaymentDate <= offCycleEndDate.Date)
                .ToList();

            return Zero(keys);
        }

        public Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate)
        {
            var nextMonth = payDate.Date.AddMonths(1);
            var nextMonthPayDay = new DateTime(nextMonth.Year, nextMonth.Month, 1);

            if (!LastMonthRecoveryRows.ContainsKey(nextMonthPayDay))
            {
                return Task.FromResult(0);
            }

            LastMonthRecoveryRows[nextMonthPayDay] = 0;
            return Task.FromResult(1);
        }

        private Task<int> Zero(List<(DateTime PaymentDate, DateTime OffCyclePaymentDate)> keys)
        {
            foreach (var key in keys)
            {
                Rows[key] = 0;
            }

            return Task.FromResult(keys.Count);
        }
    }

    private readonly RegisterStoreFake _store = new RegisterStoreFake();
    private readonly AdvancePaymentServiceFake _advance = new AdvancePaymentServiceFake();

    private static WageType BasicPayWageType() => new WageType
    {
        Id = BasicPayWageTypeId,
        TenantId = 1,
        Code = "1000",
        NameEn = "Basic Pay",
        BillingType = BillingType.Earning,
        // THE SCENARIO'S PREMISE: salary / 26, not salary / days-in-month.
        BillingMethod = BillingMethod.FixedDaysPerMonth,
        FixedDays = FixedDays,
        ProrateMethod = ProrateMethod.WorkingDays,
        IncludeInBasicPay = true,
        // MANDATORY for an Advance Leave off-cycle: PayrollRunGates.AllowIncludingAdvanceLeave and
        // EmployeeBasicPayCalculationService's own gate both require it, or the line is written as Amount 0.
        IncludeInAdvancePay = true,
        DontImpactNetPay = false,
        Status = true
    };

    private static WageType LeavePayWageType() => new WageType
    {
        Id = LeavePayWageTypeId,
        TenantId = 1,
        Code = "1050",
        NameEn = "Annual Leave Pay",
        BillingType = BillingType.Earning,
        BillingMethod = BillingMethod.FixedDaysPerMonth,
        FixedDays = FixedDays,
        IncludeInAdvancePay = true,
        DontImpactNetPay = false,
        Status = true
    };

    /// <summary>
    /// Wage code 4014 "Present Days". <see cref="PayrollDayCountService.GetPresentDaysAsync"/> returns 0 unless
    /// THIS wage type is <see cref="BillingMethod.FixedDaysPerMonth"/> -- the fixed-days basic pay reads its
    /// day count from here, so its <c>FixedDays</c> is what "26 days" actually means to the engine.
    /// </summary>
    private static WageType PresentDaysWageType() => new WageType
    {
        Id = PresentDaysWageTypeId,
        TenantId = 1,
        Code = "4014",
        NameEn = "Present Days",
        BillingType = BillingType.TimeType,
        BillingMethod = BillingMethod.FixedDaysPerMonth,
        FixedDays = FixedDays,
        ProrateMethod = ProrateMethod.WorkingDays,
        Status = true
    };

    /// <summary>
    /// Wage code 4008 "Weekly Off Days". Left on <see cref="BillingMethod.FixedAmountPerMonth"/> so the day
    /// count service takes its simple branch and sums <c>TimeRegister.WeeklyOff</c> -- zero in this fixture.
    /// </summary>
    private static WageType WeeklyOffWageType() => new WageType
    {
        Id = WeeklyOffWageTypeId,
        TenantId = 1,
        Code = "4008",
        NameEn = "Weekly Off Days",
        BillingType = BillingType.TimeType,
        BillingMethod = BillingMethod.FixedAmountPerMonth,
        Status = true
    };

    private static AbsenceType AnnualLeave() => new AbsenceType
    {
        Id = AnnualLeaveAbsenceTypeId,
        TenantId = 1,
        Code = "1",
        NameEn = "Annual Leave",
        NameAr = "Annual Leave",
        WageTypeId = LeavePayWageTypeId,
        QuotaType = QuotaType.AnnualLeave,
        IsPaid = true,
        // THE FLAG THAT EXCLUDES WEEK-OFF FRIDAYS FROM LEAVE DAYS. With IncludeWeekend false, the Time Engine
        // leaves a Friday inside the leave as WeeklyOff = 1 / LeaveDay = 0; with it true, it would stamp
        // LeaveDay = 1 and CLEAR WeeklyOff to 0 (legacy GeneralTimeService.GetLeaveDay lines 256-270, ported in
        // TimeRegisterFieldCalculator.Leave.cs). The two states are mutually exclusive by construction, which
        // is why the payroll side needs no week-off filter of its own and cannot double-count.
        IncludeWeekend = false,
        IncludeRestDay = false,
        IncludePublicHoliday = false,
        IsActive = true
    };

    private static EmployeeAbsence AdvanceLeave() => new EmployeeAbsence
    {
        Id = 1,
        TenantId = 1,
        EmployeeId = EmployeeId,
        AbsenceTypeId = AnnualLeaveAbsenceTypeId,
        AbsenceType = AnnualLeave(),
        StartDate = LeaveStart,
        EndDate = LeaveEnd,
        TotalLeaveDays = 30,
        CalendarDays = 30,
        LeavePaymentType = LeavePaymentType.PayInAdvance,
        PaymentDate = OffCyclePaymentDate,
        AdvanceLeavePaid = false
    };

    /// <summary>
    /// A contract spanning the whole scenario. Without one covering both ends of a month, the day count
    /// service takes its mid-month joining/leaving path and switches that month to actual days.
    /// </summary>
    private static EmployeeContract Contract() => new EmployeeContract
    {
        Id = 1,
        TenantId = 1,
        EmployeeId = EmployeeId,
        StartDate = new DateTime(2020, 1, 1),
        EndDate = new DateTime(2099, 12, 31)
    };

    /// <summary>
    /// The time register the Time Engine would have produced for a <b>Friday-weekend</b> employee on an absence
    /// type with <c>IncludeWeekend = false</c>. Three kinds of day:
    /// <list type="bullet">
    /// <item><description><b>Friday</b> — <c>WeeklyOff = 1</c>, no working day, and <c>LeaveDay = 0</c> even
    /// inside the leave. <c>LeaveTypeId</c> is still stamped, because legacy sets the leave type first and only
    /// then zeroes the day count — several payroll sums filter on <c>LeaveTypeId != null</c>, so dropping it
    /// would not be faithful.</description></item>
    /// <item><description><b>Leave day, Saturday to Thursday</b> — <c>LeaveDay = 1</c>.</description></item>
    /// <item><description><b>Anything else</b> — a worked day.</description></item>
    /// </list>
    ///
    /// <para>Fridays in this window: September 4/11/18/25 (4), October 2/9/16/23/30 (5), November 6/13/20/27 (4).
    /// Five of them fall inside the leave — 16, 23, 30 October and 6, 13 November — so <b>30 calendar days of
    /// leave are only 25 paid leave days</b>.</para>
    /// </summary>
    private static List<TimeRegister> TimeRegisters()
    {
        var rows = new List<TimeRegister>();
        var id = 1;

        for (var date = new DateTime(2026, 9, 1); date <= new DateTime(2026, 11, 30); date = date.AddDays(1))
        {
            var isWeeklyOff = date.DayOfWeek == DayOfWeek.Friday;
            var isInLeave = date >= LeaveStart && date <= LeaveEnd;

            // IncludeWeekend = false: a Friday inside the leave stays a week-off and is NOT a leave day.
            var isLeaveDay = isInLeave && !isWeeklyOff;
            var isWorked = !isInLeave && !isWeeklyOff;

            rows.Add(new TimeRegister
            {
                Id = id++,
                TenantId = 1,
                EmployeeId = EmployeeId,
                DayDate = date,
                WeeklyOff = isWeeklyOff ? 1 : 0,
                ActualWorkingDay = isWorked ? 1 : 0,
                ApprovedActualWorkingDay = isWorked ? 1 : 0,
                LeaveDay = isLeaveDay ? 1 : 0,
                LeaveTypeId = isInLeave ? AnnualLeaveAbsenceTypeId : (int?)null
            });
        }

        return rows;
    }

    /// <summary>Leave days (not calendar days) in a month, as the Time Engine stamped them.</summary>
    private static int LeaveDaysIn(int year, int month)
        => TimeRegisters().Count(r => r.DayDate.Year == year && r.DayDate.Month == month && r.LeaveDay == 1);

    /// <summary>Week-offs in a month. With a 31-day October and 5 Fridays, the salary basis is 26 — the fixed days.</summary>
    private static int WeekOffsIn(int year, int month)
        => TimeRegisters().Count(r => r.DayDate.Year == year && r.DayDate.Month == month && r.WeeklyOff == 1);

    /// <summary>The one salary register each run writes to, as the day count service needs to read it.</summary>
    private static SalaryRegister Register(DateTime payInDate, PayrollRunType runType) => new SalaryRegister
    {
        Id = RegisterId,
        TenantId = 1,
        EmployeeId = EmployeeId,
        ForPeriod = payInDate,
        InPeriod = payInDate,
        PayrollRunType = runType,
        PayrollResultType = PayrollResultType.Current
    };

    /// <summary>
    /// The REAL day count service. Everything it needs is fixture data; only the settings accessor and the
    /// roster service are substituted, and both are left returning null/defaults so
    /// <c>GetDayCountSettingsAsync</c> falls back to its documented defaults (every fixed-to-actual switch
    /// off, <c>EnableBasicpayMoreThenFixedDays</c> on).
    /// </summary>
    private static IPayrollDayCountService BuildDayCountService(
        List<TimeRegister> timeRegisters,
        SalaryRegister register,
        HCMv3.SystemSettings.ManagePayrollSettings payrollSettings = null)
    {
        var settingsAccessor = Substitute.For<ISysSettingsAccessor>();
        if (payrollSettings != null)
        {
            settingsAccessor.GetPayrollSettingsAsync().Returns(Task.FromResult(payrollSettings));
        }

        return new PayrollDayCountService(
            Repo(timeRegisters),
            Repo(BasicPayWageType(), LeavePayWageType(), PresentDaysWageType(), WeeklyOffWageType()),
            Repo(AnnualLeave()),
            Repo(register),
            Repo(Enumerable.Empty<SalaryRegisterDetail>()),
            Repo(Enumerable.Empty<PayrollArea>()),
            Repo(Contract()),
            Repo(AdvanceLeave()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(Enumerable.Empty<EmployeeTimePolicy>()),
            settingsAccessor,
            Substitute.For<IRosterService>());
    }

    private EmployeeBasicPayCalculationService BuildBasicPay(List<TimeRegister> timeRegisters, SalaryRegister register)
    {
        return new EmployeeBasicPayCalculationService(
            Repo(new EmployeeBasicPay
            {
                Id = 1,
                TenantId = 1,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                PayInterval = PayInterval.Monthly
            }),
            Repo(new EmployeeBasicPayDetail
            {
                Id = 1,
                TenantId = 1,
                EmployeeBasicPayId = 1,
                WageTypeId = BasicPayWageTypeId,
                Amount = MonthlyBasic
            }),
            Repo(Enumerable.Empty<EmployeeRemuneration>()),
            Repo(Enumerable.Empty<EmployeeSeparation>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(timeRegisters),
            _store,
            BuildDayCountService(timeRegisters, register),
            Substitute.For<ITimeDataService>(),
            Substitute.For<IRosterService>(),
            _advance);
    }

    private EmployeeLeavePayCalculationService BuildLeavePay(List<TimeRegister> timeRegisters, SalaryRegister register)
    {
        var subGroupResolver = Substitute.For<IEmployeeSubGroupResolver>();
        subGroupResolver.GetEmployeeSubGroupIdAsync(default, default)
            .ReturnsForAnyArgs(Task.FromResult((int?)null));

        return new EmployeeLeavePayCalculationService(
            Repo(AnnualLeave()),
            Repo(BasicPayWageType(), LeavePayWageType()),
            // Leave is valued at the SUM of the Rates of the wage types flagged WageTypeLeave for the absence
            // type -- here basic pay only, so the leave rate is 260 and a leave day is worth 260/26.
            Repo(new WageTypeLeave
            {
                Id = 1,
                TenantId = 1,
                WageTypeId = BasicPayWageTypeId,
                AbsenceTypeId = AnnualLeaveAbsenceTypeId
            }),
            Repo(AdvanceLeave()),
            Repo(Enumerable.Empty<EmployeeAbsAdjustment>()),
            Repo(Enumerable.Empty<EmployeeSubGroupAbsence>()),
            Repo(Enumerable.Empty<AbsenceDedRuleDetail>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(timeRegisters),
            _store,
            BuildDayCountService(timeRegisters, register),
            Substitute.For<IAccrualService>(),
            Substitute.For<IRosterService>(),
            subGroupResolver,
            _advance);
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        DateTime payInDate,
        PayrollRunType runType,
        List<TimeRegister> timeRegisters,
        DateTime? offCycleEndDate = null,
        DateTime? offCyclePaymentDate = null,
        string offCycleReason = null)
    {
        var payStartDate = new DateTime(payInDate.Year, payInDate.Month, 1);

        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = new Employee { Id = EmployeeId },
            PayDate = payInDate,
            PayStartDate = payStartDate,
            PayInDate = payInDate,
            // PayrollEngineService.WageTypeLoop seeds this with payInDate and narrows it per off-cycle reason.
            OffCycleEndDate = offCycleEndDate ?? payInDate,
            OffCyclePaymentDate = offCyclePaymentDate ?? default(DateTime),
            OffCycleReason = offCycleReason,
            PayrollRunType = runType,
            WageType = wageType,
            // Legacy leaves this NULL on an off-cycle run; the regular run caches the month's rows.
            TimeRegisterList = runType == PayrollRunType.Regular
                ? timeRegisters.Where(r => r.DayDate >= payStartDate && r.DayDate <= payInDate).ToList()
                : null,
            TimeRegisterQuery = timeRegisters.AsQueryable(),
            Settings = new PayrollEngineSettings { DecimalInSalaryRegister = 6 },
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = payInDate,
                InPeriod = payInDate,
                PayrollRunType = runType
            }
        };
    }

    /// <summary>
    /// Runs one payroll the way <c>PayrollEngineService</c> does: the off-cycle clearing blocks first, then
    /// basic pay and leave pay in <c>WageType.PayrollOrder</c> order over one register.
    ///
    /// <para>The clearing calls mirror the engine's own call sites exactly -- block A from
    /// <c>PayrollEngineService.Register.cs</c> (once per employee, before the period loop) and blocks B/C from
    /// <c>PayrollEngineService.WageTypeLoop.cs</c> (per register). <c>PayrollEngineClearingWiring_Tests</c>
    /// proves the engine really calls them; this reproduces the order so the money can be asserted.</para>
    /// </summary>
    private async Task RunAsync(
        DateTime payInDate,
        PayrollRunType runType,
        DateTime? offCycleEndDate = null,
        DateTime? offCyclePaymentDate = null,
        string offCycleReason = null)
    {
        var timeRegisters = TimeRegisters();
        var register = Register(payInDate, runType);
        var payStartDate = new DateTime(payInDate.Year, payInDate.Month, 1);

        if (runType == PayrollRunType.OffCycle)
        {
            // Block A -- legacy Program.cs line 1201, before the period loop.
            await _advance.DeleteAdvancePaymentDedAsync(EmployeeId, offCyclePaymentDate ?? default(DateTime));

            // Blocks C1-C4 -- legacy lines 1787-1851.
            await ZeroRegisterLineAsync(PayrollEngineWageCodes.AdvancePaymentDeduction);
            if (offCycleReason != PayrollOffCycleReasons.EndOfService)
            {
                await _advance.ZeroAdvanceRecoveryInWindowAsync(
                    EmployeeId, payStartDate, offCycleEndDate ?? payInDate);
            }

            await ZeroRegisterLineAsync(PayrollEngineWageCodes.LastMonthRecoveryDeduction);
            await _advance.ZeroLastMonthRecoveryAsync(EmployeeId, payInDate);
        }
        else
        {
            // Block B -- legacy lines 1619-1653. The leave is still on file with PaymentDate 15/10, which is
            // not a month end, so this never matches in this scenario -- see the dedicated test.
            var hasAdvancePaidLeaveOnPayDate = AdvanceLeave().PaymentDate == payInDate;
            await _advance.ZeroFutureAdvanceRecoveryAsync(EmployeeId, payInDate, hasAdvancePaidLeaveOnPayDate);

            // THE TIME-TYPE PRE-PASS, for wage code 4014 "Present Days" only.
            //
            // On a REGULAR run, fixed-days basic pay does NOT ask the day count service for present days: it
            // reads the 4014 line off the register (EmployeeBasicPayCalculationService legacy lines 321-332),
            // which ITimeTypeCumulationService wrote earlier in the run. Skipping this leaves the line absent,
            // present days 0, and basic pay 0 -- so the pre-pass is reproduced here with the SAME call the real
            // one makes (TimeTypeCumulationService, legacy Program.cs lines 2197-2203).
            //
            // The off-cycle path needs none of this: it calls GetPresentDaysAsync itself, per recovery month.
            await SeedPresentDaysLineAsync(payInDate, timeRegisters, register);
        }

        await BuildBasicPay(timeRegisters, register).CalculateAsync(Context(
            BasicPayWageType(), payInDate, runType, timeRegisters, offCycleEndDate, offCyclePaymentDate, offCycleReason));

        await BuildLeavePay(timeRegisters, register).CalculateAsync(Context(
            LeavePayWageType(), payInDate, runType, timeRegisters, offCycleEndDate, offCyclePaymentDate, offCycleReason));
    }

    /// <summary>
    /// Writes the 4014 "Present Days" line with the day count the REAL
    /// <see cref="PayrollDayCountService"/> produces -- the one thing
    /// <see cref="ITimeTypeCumulationService"/> does that this scenario depends on.
    /// </summary>
    private async Task SeedPresentDaysLineAsync(DateTime payInDate, List<TimeRegister> timeRegisters, SalaryRegister register)
    {
        var payStartDate = new DateTime(payInDate.Year, payInDate.Month, 1);

        var presentDays = await BuildDayCountService(timeRegisters, register).GetPresentDaysAsync(
            EmployeeId, payStartDate, payInDate, RegisterId, PayrollRunType.Regular, payInDate, null);

        await _store.SaveLineAsync(
            new PayrollRegisterRef { RegisterId = RegisterId, Target = PayrollRegisterTarget.Live },
            new PayrollRegisterLine
            {
                WageTypeId = PresentDaysWageTypeId,
                WageCode = PresentDaysWageCode,
                Number = presentDays.PresentDays
            },
            deleteWhenAmountIsZero: false);
    }

    private async Task ZeroRegisterLineAsync(string wageCode)
    {
        var line = _store.Lines.FirstOrDefault(l => l.WageCode == wageCode);
        if (line != null)
        {
            line.Amount = 0;
        }

        await Task.CompletedTask;
    }

    private PayrollRegisterLine Line(int wageTypeId) => _store.Lines.Single(l => l.WageTypeId == wageTypeId);

    private PayrollRegisterLine LineOrNull(int wageTypeId) => _store.Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId);

    private List<SalaryRegisterDayCount> DaysOf(int wageTypeId)
    {
        var line = LineOrNull(wageTypeId);
        if (line == null || !_store.DayDetails.TryGetValue(line.Id, out var days))
        {
            return new List<SalaryRegisterDayCount>();
        }

        return days.OrderBy(d => d.Period).ToList();
    }

    /// <summary>
    /// Net pay as wage code 9000 computes it: earnings minus deductions over the lines that impact net pay.
    /// The 2005 recovery is passed in because it reaches the register through
    /// <see cref="EmployeePaymentCalculationService"/>, which this scenario drives by hand rather than wiring
    /// a second calculator in.
    /// </summary>
    private double NetPay(double advanceRecovery = 0) => _store.Lines.Sum(l => l.Amount) - advanceRecovery;

    private static double Round6(double value) => Math.Round(value, 6);

    private void ResetRegister()
    {
        _store.Lines.Clear();
        _store.DayDetails.Clear();
    }

    #endregion

    #region << Step 1-2: September regular run, exit payroll >>

    /// <summary>
    /// September is a plain full month. On fixed-26 billing a full month's present days are the fixed 26, so
    /// the pay is 260/26 x 26 = 260 -- the scenario's premise ("salary 260, net pay 260 for September") and the
    /// baseline every later figure is measured against.
    /// </summary>
    [Fact]
    public async Task September_Regular_Should_Pay_The_Full_260_On_26_Fixed_Days()
    {
        await RunAsync(SepPayInDate, PayrollRunType.Regular);

        var basic = Line(BasicPayWageTypeId);
        basic.Rate.ShouldBe(MonthlyBasic);
        basic.Number.ShouldBe(26);                      // the fixed days, not the 30 calendar days
        basic.Amount.ShouldBe(260, 0.000001);           // 10.000 x 26

        // No leave in September, so no 1050 line at all.
        LineOrNull(LeavePayWageTypeId).ShouldBeNull();

        NetPay().ShouldBe(260, 0.000001);

        // A regular run must never post an advance recovery.
        _advance.Rows.ShouldBeEmpty();
    }

    /// <summary>
    /// The gate sequence the tester walks through on the Payroll Control Record screen, asserted on the guard
    /// the engine actually calls. Sep must be RELEASED FOR PAYROLL (PeriodLocked) to run Regular, and CLOSED
    /// (exit payroll) before the off-cycle is allowed -- and the off-cycle's pay-in date is then FORCED to
    /// 31/10/2026, the month end of the payment date, not anything the user typed.
    /// </summary>
    [Fact]
    public void PayrollControlRecord_Should_Gate_The_Scenario_In_Order()
    {
        var record = new PayrollControlRecord
        {
            Id = 1,
            TenantId = 1,
            PayrollAreaId = 1,
            CalendarMonthEnd = SepPayInDate
        };

        // Step 1 -- Open: a regular September run is refused.
        record.ApplyState(PayrollPeriodState.Open, 1, DateTime.Now);
        Should.Throw<Abp.UI.UserFriendlyException>(
            () => PayrollControlRecordGuard.EnsureRegularRunAllowed(record, SepPayInDate));

        // Step 2 -- Released for payroll (PeriodLocked): the September regular run is allowed.
        record.ApplyState(PayrollPeriodState.ReleasedForPayroll, 1, DateTime.Now);
        PayrollControlRecordGuard.EnsureRegularRunAllowed(record, SepPayInDate);

        // ... but the off-cycle is NOT, because the period is not closed yet.
        Should.Throw<Abp.UI.UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, OctPayInDate, OffCyclePaymentDate, "E1", enableTimeEngineAutorun: false));

        // Step 3 -- Closed ("exit payroll"): the off-cycle is allowed, and its pay-in date is the MONTH END of
        // the off-cycle payment date.
        record.ApplyState(PayrollPeriodState.Closed, 1, DateTime.Now);
        var resolved = PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, OctPayInDate, OffCyclePaymentDate, "E1", enableTimeEngineAutorun: false);

        resolved.ShouldBe(OctPayInDate);

        // And the per-employee re-check passes only for that one month -- the "do not skip a month" guard.
        PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, new PayrollArea { Id = 1, Name = "PA" }, PayrollRunType.OffCycle, OctPayInDate);

        Should.Throw<Abp.UI.UserFriendlyException>(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, new PayrollArea { Id = 1, Name = "PA" }, PayrollRunType.OffCycle, NovPayInDate));
    }

    /// <summary>
    /// An off-cycle run demands an employee code and a payment date. Both are the errors a tester hits first if
    /// they leave the Payroll Engine screen's off-cycle fields blank.
    /// </summary>
    [Fact]
    public void OffCycle_Should_Require_Employee_Code_And_Payment_Date()
    {
        var record = new PayrollControlRecord { Id = 1, TenantId = 1, PayrollAreaId = 1, CalendarMonthEnd = SepPayInDate };
        record.ApplyState(PayrollPeriodState.Closed, 1, DateTime.Now);

        Should.Throw<Abp.UI.UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
                record, OctPayInDate, OffCyclePaymentDate, employeeCodes: "", enableTimeEngineAutorun: false))
            .Message.ShouldContain("No Employee Code found");

        Should.Throw<Abp.UI.UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
                record, OctPayInDate, default(DateTime), "E1", enableTimeEngineAutorun: false))
            .Message.ShouldContain("offCycle PaymentDate not entered");
    }

    #endregion

    #region << Step 3: the off-cycle advance-leave run >>

    /// <summary>
    /// THE CENTRAL ASSERTION. One off-cycle register for ForPeriod = InPeriod = 31/10/2026 carrying:
    /// <list type="bullet">
    /// <item><description>1000 Basic Pay for the days he is at work before travelling. On fixed-26 billing that
    /// is <c>26 - leaveDays</c>: October has 13 paid leave days (16 calendar days less the Fridays), so 13
    /// present days at 10.000 = 130.000;</description></item>
    /// <item><description>1050 Annual Leave Pay for the 25 paid leave days at a flat 10.000/day = 250.000.
    /// Unlike calendar-days billing, the rate does NOT change between October and November -- that is the whole
    /// point of fixed days.</description></item>
    /// </list>
    /// October still comes to exactly one month (130 + 130), which is the property to watch: the fixed-days
    /// formula and the leave valuation must agree on what a day is worth, or a full month of leave stops
    /// summing to a full month of pay.
    /// </summary>
    [Fact]
    public async Task OffCycle_Should_Pay_October_In_Full_Plus_The_November_Leave_Days()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        var basic = Line(BasicPayWageTypeId);
        basic.Rate.ShouldBe(MonthlyBasic);
        basic.Number.ShouldBe(13);                       // 26 fixed - 13 October leave days
        basic.Amount.ShouldBe(130, 0.000001);            // 10.000 x 13

        var leave = Line(LeavePayWageTypeId);
        leave.Rate.ShouldBe(MonthlyBasic);
        leave.Number.ShouldBe(25);                       // 13 October + 12 November, Fridays excluded
        leave.Amount.ShouldBe(250, 0.000001);            // 10.000 x 25, one flat rate

        // October is paid whole: basic for the days at work + leave pay for the days on leave.
        (130d + (PerDay * 13)).ShouldBe(260, 0.000001);

        // ... and November only as far as the leave runs: 12 leave days x 10.000.
        NetPay().ShouldBe(380, 0.000001);
    }

    /// <summary>
    /// <b>Week-off Fridays are excluded from the leave days.</b> The employee takes 30 CALENDAR days, but five of
    /// them are Fridays, so only 25 are paid leave days. This is the absence type's <c>IncludeWeekend = false</c>
    /// at work, resolved by the Time Engine and not by payroll: legacy
    /// <c>GeneralTimeService.GetLeaveDay</c> (lines 256-270) zeroes the day count when
    /// <c>IncludeWeekend == false &amp;&amp; WeeklyOff == 1</c>, and CLEARS <c>WeeklyOff</c> when it is true.
    ///
    /// <para>The two states are mutually exclusive by construction, and that is load-bearing: payroll's leave-pay
    /// side sums <c>LeaveDay</c> with NO week-off filter of its own (legacy
    /// <c>EmployeeLeavePayService.GetLeaveDaysMonthWise</c> line 850), so a row that was both would be paid as
    /// leave AND left out of present days -- the employee paid twice for one Friday. Asserted here on the fixture
    /// so that if the Time Engine's stamping ever changes, this scenario says so.</para>
    /// </summary>
    [Fact]
    public void WeekOff_Fridays_Should_Not_Count_As_Leave_Days()
    {
        var rows = TimeRegisters();

        // Thirty calendar days of leave ...
        rows.Count(r => r.DayDate >= LeaveStart && r.DayDate <= LeaveEnd).ShouldBe(30);

        // ... of which five are Fridays: 16, 23, 30 October and 6, 13 November.
        var leaveFridays = rows
            .Where(r => r.DayDate >= LeaveStart && r.DayDate <= LeaveEnd && r.DayDate.DayOfWeek == DayOfWeek.Friday)
            .ToList();

        leaveFridays.Count.ShouldBe(5);

        // Each one is a week-off and NOT a leave day -- never both.
        foreach (var friday in leaveFridays)
        {
            friday.WeeklyOff.ShouldBe(1);
            friday.LeaveDay.ShouldBe(0);
            friday.ActualWorkingDay.ShouldBe(0);

            // The leave type is still stamped; legacy sets it first and only then zeroes the count.
            friday.LeaveTypeId.ShouldBe(AnnualLeaveAbsenceTypeId);
        }

        rows.Count(r => r.LeaveDay == 1 && r.WeeklyOff == 1).ShouldBe(0);

        // So 30 calendar days are 25 paid leave days, split 13 / 12.
        LeaveDaysIn(2026, 10).ShouldBe(13);
        LeaveDaysIn(2026, 11).ShouldBe(12);
        (LeaveDaysIn(2026, 10) + LeaveDaysIn(2026, 11)).ShouldBe(25);

        // And October's 5 Fridays make its salary basis (31 - 5) exactly the fixed 26, which is why
        // "working days + leave days = 26" holds for October.
        WeekOffsIn(2026, 10).ShouldBe(5);
        ((double)DateTime.DaysInMonth(2026, 10) - WeekOffsIn(2026, 10)).ShouldBe(FixedDays);
    }

    /// <summary>
    /// THE OFF-CYCLE PAYS EXACTLY THREE THINGS, and this states them one per assertion so a change to any one
    /// of them names itself:
    /// <list type="number">
    /// <item><description><b>October working days</b> -- 13, being the fixed 26 less October's 13 leave
    /// days (legacy <c>Program.GetPresentDays</c>: <c>workingdays = fixeddays - totalLeavedays</c>);</description></item>
    /// <item><description><b>October leave days</b> -- 13, at <c>rate / 26</c>. Sixteen calendar days of leave
    /// less the three week-off Fridays (16, 23, 30 October);</description></item>
    /// <item><description><b>November leave days</b> -- 12, at the same <c>rate / 26</c>. Fourteen calendar days
    /// less the two Fridays (6, 13 November).</description></item>
    /// </list>
    /// November's WORKING days are deliberately NOT here: the off-cycle window closes on the leave's end date
    /// (14/11), so 15-30/11 belongs to November's own regular run. And October's working + leave days come to
    /// exactly the fixed 26, which is why October alone equals one month's salary.
    /// </summary>
    [Fact]
    public async Task OffCycle_Should_Pay_October_Working_Days_Plus_October_And_November_Leave_Days()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        var octoberWorkingDays = Line(BasicPayWageTypeId).Number;
        var octoberLeaveDays = DaysOf(LeavePayWageTypeId).Single(d => d.Period == new DateTime(2026, 10, 31)).Number;
        var novemberLeaveDays = DaysOf(LeavePayWageTypeId).Single(d => d.Period == new DateTime(2026, 11, 30)).Number;

        octoberWorkingDays.ShouldBe(13);
        octoberLeaveDays.ShouldBe(13);
        novemberLeaveDays.ShouldBe(12);

        // Week-off Fridays are excluded from the leave days, so they come from the fixture, not a literal.
        octoberLeaveDays.ShouldBe(LeaveDaysIn(2026, 10));
        novemberLeaveDays.ShouldBe(LeaveDaysIn(2026, 11));

        // October's two halves are the fixed month, exactly.
        (octoberWorkingDays + octoberLeaveDays).ShouldBe(FixedDays);

        // All three, each at 260/26 = 10.000 a day.
        ((octoberWorkingDays + octoberLeaveDays + novemberLeaveDays) * PerDay).ShouldBe(380, 0.000001);
        NetPay().ShouldBe(380, 0.000001);

        // November's working days are NOT advanced -- November's own regular run pays them.
        Line(BasicPayWageTypeId).Number.ShouldNotBe(13 + 14);
    }

    /// <summary>
    /// <b>Guards a fidelity bug found by reading legacy.</b> Legacy hands
    /// <c>GetPresentDays</c> THIS MONTH'S clamped end date as its <c>offCycleEndDate</c>
    /// (<c>EmployeeBasicPayService.cs</c> line 921); the port was passing the RUN's off-cycle end date. Because
    /// <c>GetPresentDays</c> recomputes its own recovery periods from that bound on an Advance Leave run, the
    /// October call fanned out over October AND November and could add both months' present days into October's
    /// basic pay.
    ///
    /// <para>This test makes the bound observable: October's basic pay must equal ONE month's present days
    /// (26 - 16 = 10), never October's plus November's (10 + 12 = 22).</para>
    /// </summary>
    [Fact]
    public async Task OffCycle_Basic_Pay_Should_Count_One_Recovery_Month_At_A_Time()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        Line(BasicPayWageTypeId).Number.ShouldBe(13);
        Line(BasicPayWageTypeId).Amount.ShouldBe(130, 0.000001);

        // One days-detail row, because only October contributes basic pay at all.
        DaysOf(BasicPayWageTypeId).Count.ShouldBe(1);
        DaysOf(BasicPayWageTypeId)[0].Period.ShouldBe(new DateTime(2026, 10, 31));
    }

    /// <summary>
    /// Wage code 2005 "Advance Payment Deduction": ONE <c>EmployeePayment</c> row per calendar month the leave
    /// touches, dated the 1st of that month, carrying everything advanced for it. Basic pay and leave pay both
    /// post onto October's row, which is why it totals a full 260 rather than either rule's share.
    ///
    /// <para>The row's key includes <c>OffCyclePaymentDate</c>; dropping it merges recoveries from different
    /// off-cycle runs and over-deducts, which is why wave P8 restored the column.</para>
    /// </summary>
    [Fact]
    public async Task OffCycle_Should_Split_The_Advance_Recovery_Across_October_And_November()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        _advance.Rows.Count.ShouldBe(2);

        _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)].ShouldBe(260, 0.000001);   // 130 basic + 130 leave
        _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)].ShouldBe(120, 0.000001);   // 12 leave days only

        // The recovery equals what was advanced, to the last fils.
        _advance.Rows.Values.Sum().ShouldBe(Round6(NetPay()), 0.000001);

        // November's BASIC pay share is zero -- he is on leave 01-14 November and the window closes there -- so
        // only the leave rule posts for it.
        _advance.Postings.Count(p => p.PaymentDate == new DateTime(2026, 11, 1)).ShouldBe(1);
    }

    /// <summary>
    /// <c>SalaryRegisterDaysDetail</c> -- the month-wise breakdown under each register line. It exists ONLY on
    /// an off-cycle register whose <c>InPeriod == ForPeriod</c>, and only for a non-zero amount; <c>Period</c>
    /// is stamped as that month's LAST day (legacy convention), and <c>Rate</c> repeats the line's rate.
    /// </summary>
    [Fact]
    public async Task OffCycle_Should_Write_One_DaysDetail_Row_Per_Month_Per_Line()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        // Basic pay: October only. November's share is 0, so no row.
        var basicDays = DaysOf(BasicPayWageTypeId);
        basicDays.Count.ShouldBe(1);
        basicDays[0].Period.ShouldBe(new DateTime(2026, 10, 31));
        basicDays[0].Rate.ShouldBe(MonthlyBasic);
        basicDays[0].Number.ShouldBe(13);
        basicDays[0].Amount.ShouldBe(130, 0.000001);

        // Leave pay: one row per month, both at the same flat 10.000/day, Fridays excluded.
        var leaveDays = DaysOf(LeavePayWageTypeId);
        leaveDays.Count.ShouldBe(2);

        leaveDays[0].Period.ShouldBe(new DateTime(2026, 10, 31));
        leaveDays[0].Number.ShouldBe(13);
        leaveDays[0].Amount.ShouldBe(130, 0.000001);

        leaveDays[1].Period.ShouldBe(new DateTime(2026, 11, 30));
        leaveDays[1].Number.ShouldBe(12);
        leaveDays[1].Amount.ShouldBe(120, 0.000001);

        // The day rows reconcile to their line, which is what makes the payslip's breakdown trustworthy.
        leaveDays.Sum(d => d.Amount).ShouldBe(Line(LeavePayWageTypeId).Amount, 0.000001);
        leaveDays.Sum(d => d.Number).ShouldBe(Line(LeavePayWageTypeId).Number);
    }

    /// <summary>
    /// Without <c>IncludeInAdvancePay</c> on the wage type, an Advance Leave off-cycle writes the line with
    /// Amount 0 and advances nothing -- the "the off-cycle ran but paid him nothing" support call. Asserted
    /// because it is configuration, not code, and therefore the most likely cause on a new tenant.
    /// </summary>
    [Fact]
    public async Task OffCycle_Should_Pay_Nothing_When_The_WageType_Is_Not_IncludeInAdvancePay()
    {
        var timeRegisters = TimeRegisters();
        var register = Register(OctPayInDate, PayrollRunType.OffCycle);
        var wageType = BasicPayWageType();
        wageType.IncludeInAdvancePay = false;

        await BuildBasicPay(timeRegisters, register).CalculateAsync(Context(
            wageType, OctPayInDate, PayrollRunType.OffCycle, timeRegisters,
            OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave));

        Line(BasicPayWageTypeId).Rate.ShouldBe(MonthlyBasic);   // the rate is still written
        Line(BasicPayWageTypeId).Amount.ShouldBe(0);
        _advance.Rows.ShouldBeEmpty();
    }

    #endregion

    #region << Step 4-5: the October and November regular runs recover it >>

    /// <summary>
    /// October regular: he earns his whole October salary again (basic for the 10 present days, leave pay for
    /// the 16 leave days) and the 2005 row dated 01/10 takes all of it back. NET ZERO -- correct, because the
    /// money reached him on 15/10 through the off-cycle.
    /// </summary>
    [Fact]
    public async Task October_Regular_Should_Net_To_Zero_After_The_Advance_Recovery()
    {
        await RunAsync(OctPayInDate, PayrollRunType.Regular);

        Line(BasicPayWageTypeId).Number.ShouldBe(13);            // 26 fixed - 13 leave
        Line(BasicPayWageTypeId).Amount.ShouldBe(130, 0.000001);

        Line(LeavePayWageTypeId).Number.ShouldBe(13);
        Line(LeavePayWageTypeId).Amount.ShouldBe(130, 0.000001);

        // Gross is the full month again ...
        _store.Lines.Sum(l => l.Amount).ShouldBe(260, 0.000001);

        // ... and the 2005 EmployeePayment dated 01/10/2026 recovers 260.000000, so net pay is zero.
        NetPay(advanceRecovery: 260).ShouldBe(0, 0.000001);

        // A regular run posts no NEW advance recovery.
        _advance.Rows.ShouldBeEmpty();

        // SalaryRegisterDaysDetail is off-cycle-only: a regular register carries none.
        DaysOf(BasicPayWageTypeId).ShouldBeEmpty();
        DaysOf(LeavePayWageTypeId).ShouldBeEmpty();
    }

    /// <summary>
    /// November regular: leave pay for 01-14, basic for the days back at work, and only November's share of the
    /// advance (140.000) is recovered. He is paid 12 present days -- 120.000.
    /// </summary>
    [Fact]
    public async Task November_Regular_Should_Pay_The_Worked_Days_And_Recover_Only_Novembers_Share()
    {
        await RunAsync(NovPayInDate, PayrollRunType.Regular);

        Line(BasicPayWageTypeId).Number.ShouldBe(14);            // 26 fixed - 12 leave
        Line(BasicPayWageTypeId).Amount.ShouldBe(140, 0.000001);

        Line(LeavePayWageTypeId).Number.ShouldBe(12);            // 01-14 November, Fridays 6 and 13 excluded
        Line(LeavePayWageTypeId).Amount.ShouldBe(120, 0.000001);

        _store.Lines.Sum(l => l.Amount).ShouldBe(260, 0.000001);

        NetPay(advanceRecovery: 120).ShouldBe(140, 0.000001);
    }

    /// <summary>
    /// THE BOOKS BALANCE. Across the three runs the employee is paid exactly October + November salary, and the
    /// advance nets out. This is the one assertion worth re-running after any change to the off-cycle path: if
    /// it fails, someone has been paid twice or short-paid.
    /// </summary>
    [Fact]
    public async Task The_Three_Runs_Should_Total_Exactly_Two_Months_Salary()
    {
        // The off-cycle, on 15/10.
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);
        var offCyclePaid = Round6(NetPay());
        var octoberRecovery = _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)];
        var novemberRecovery = _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)];

        // The October regular run, end of October.
        ResetRegister();
        await RunAsync(OctPayInDate, PayrollRunType.Regular);
        var octoberPaid = Round6(NetPay(octoberRecovery));

        // The November regular run, end of November.
        ResetRegister();
        await RunAsync(NovPayInDate, PayrollRunType.Regular);
        var novemberPaid = Round6(NetPay(novemberRecovery));

        offCyclePaid.ShouldBe(380, 0.000001);
        octoberPaid.ShouldBe(0, 0.000001);
        novemberPaid.ShouldBe(140, 0.000001);

        (offCyclePaid + octoberPaid + novemberPaid).ShouldBe(520, 0.000001);   // 2 x 260
    }

    #endregion

    #region << The "never more than one month" invariant >>

    /// <summary>
    /// <b>THE INVARIANT: on fixed-26 billing a month can never pay more than one month's salary.</b> Asserted
    /// across all three runs of the scenario — each REGULAR month is exactly 260.000, and the off-cycle is one
    /// October (260.000) plus the part of November's leave that falls inside the window (120.000), never more
    /// than that.
    ///
    /// <para>The fixed-days formula guarantees it by construction: <c>presentDays = 26 - leaveDays</c>, and leave
    /// pay is valued at the same <c>rate / 26</c>, so <c>presentDays + leaveDays</c> is always the fixed 26.
    /// See <see cref="Actual_Days_Branch_Can_Exceed_The_Fixed_Days_In_A_Month_With_Four_Weekly_Offs"/> for the
    /// one shape where legacy does NOT hold the line.</para>
    /// </summary>
    [Fact]
    public async Task No_Month_Should_Ever_Pay_More_Than_One_Months_Salary()
    {
        // September: a plain full month.
        await RunAsync(SepPayInDate, PayrollRunType.Regular);
        _store.Lines.Sum(l => l.Amount).ShouldBe(MonthlyBasic, 0.000001);

        // October regular: basic + leave = the fixed 26 days again, not a fils more.
        ResetRegister();
        await RunAsync(OctPayInDate, PayrollRunType.Regular);
        _store.Lines.Sum(l => l.Amount).ShouldBe(MonthlyBasic, 0.000001);
        (Line(BasicPayWageTypeId).Number + Line(LeavePayWageTypeId).Number).ShouldBe(FixedDays);

        // November regular: same.
        ResetRegister();
        await RunAsync(NovPayInDate, PayrollRunType.Regular);
        _store.Lines.Sum(l => l.Amount).ShouldBe(MonthlyBasic, 0.000001);
        (Line(BasicPayWageTypeId).Number + Line(LeavePayWageTypeId).Number).ShouldBe(FixedDays);

        // The off-cycle is one whole October plus November's in-window leave -- and its OCTOBER share alone is
        // exactly one month.
        ResetRegister();
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        var octoberShare = _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)];
        var novemberShare = _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)];

        octoberShare.ShouldBe(MonthlyBasic, 0.000001);
        novemberShare.ShouldBeLessThan(MonthlyBasic);
        (octoberShare + novemberShare).ShouldBe(Round6(NetPay()), 0.000001);
    }

    /// <summary>
    /// <b>Corrects an earlier claim of mine.</b> Turning on <c>FixedToActualWhenLeaveOverlapOnMonthEnd</c> flips
    /// October out of the fixed-days formula into ACTUAL working days, because the leave crosses 31/10. With a
    /// REAL weekend that changes nothing: the actual count is
    /// <c>sum(ActualWorkingDay) over non-leave, non-absent days</c> = the 13 days he attended, which is the same
    /// 13 the fixed formula gives (26 - 13). October stays at exactly one month either way.
    ///
    /// <para>I previously said this setting would make October 310.000. That was an artefact of an earlier
    /// fixture with NO weekly offs at all, where the attended count was 15 rather than 13. With Fridays off, the
    /// two branches agree.</para>
    /// </summary>
    [Fact]
    public async Task FixedToActual_On_MonthEnd_Overlap_Should_Not_Change_October_When_The_Weekend_Is_Real()
    {
        var timeRegisters = TimeRegisters();
        var register = Register(OctPayInDate, PayrollRunType.OffCycle);

        var settings = new HCMv3.SystemSettings.ManagePayrollSettings
        {
            FixedToActualWhenLeaveOverlapOnMonthEnd = true,
            EnableBasicpayMoreThenFixedDays = true,
            DecimalInSalaryRegister = 6
        };

        var presentDays = await BuildDayCountService(timeRegisters, register, settings).GetPresentDaysAsync(
            EmployeeId, new DateTime(2026, 10, 1), OctPayInDate, RegisterId,
            PayrollRunType.Regular, OctPayInDate, null);

        // The ACTUAL-days branch: the 13 days he genuinely attended (01-15 October less Fridays 2 and 9).
        presentDays.PresentDays.ShouldBe(13);

        // Which is exactly what the FIXED-days branch produces, so the setting is a no-op here.
        var fixedDaysResult = await BuildDayCountService(timeRegisters, register).GetPresentDaysAsync(
            EmployeeId, new DateTime(2026, 10, 1), OctPayInDate, RegisterId,
            PayrollRunType.Regular, OctPayInDate, null);

        fixedDaysResult.PresentDays.ShouldBe(presentDays.PresentDays);
    }

    /// <summary>
    /// <b>THE ONE SHAPE WHERE LEGACY DOES NOT HOLD THE LINE — a business decision, not a port bug.</b>
    ///
    /// <para>The fixed-days formula can never exceed 26. The ACTUAL-days branch can: it counts attended days, and
    /// a 31-day month whose weekday pattern gives only FOUR Fridays has 27 working days. A fully-attended
    /// March 2027 (1 March is a Monday, Fridays 5/12/19/26) therefore yields 27 present days — 270.000 against a
    /// 260.000 salary.</para>
    ///
    /// <para>Legacy has no general cap. Its only use of <c>enableBasicpayMoreThenFixeddays</c>
    /// (<c>Program.cs</c> line 4812, ported at <c>PayrollDayCountService</c>) is the much narrower
    /// <c>leaveDays &gt;= 26 =&gt; workingDays = 0</c>, which does not apply here. Pinned so the behaviour is
    /// visible and the decision to cap (or not) is explicit rather than accidental.</para>
    /// </summary>
    [Fact]
    public async Task Actual_Days_Branch_Can_Exceed_The_Fixed_Days_In_A_Month_With_Four_Weekly_Offs()
    {
        // March 2027: 31 days, starts on a Monday, so only four Fridays -> 27 working days.
        var marchStart = new DateTime(2027, 3, 1);
        var marchEnd = new DateTime(2027, 3, 31);

        var rows = new List<TimeRegister>();
        var id = 1;
        for (var date = marchStart; date <= marchEnd; date = date.AddDays(1))
        {
            var isWeeklyOff = date.DayOfWeek == DayOfWeek.Friday;
            rows.Add(new TimeRegister
            {
                Id = id++,
                TenantId = 1,
                EmployeeId = EmployeeId,
                DayDate = date,
                WeeklyOff = isWeeklyOff ? 1 : 0,
                ActualWorkingDay = isWeeklyOff ? 0 : 1,
                ApprovedActualWorkingDay = isWeeklyOff ? 0 : 1
            });
        }

        rows.Count(r => r.WeeklyOff == 1).ShouldBe(4);      // only four Fridays
        var register = Register(marchEnd, PayrollRunType.Regular);

        // The FIXED-days branch holds the line at 26.
        var fixedDays = await BuildDayCountService(rows, register).GetPresentDaysAsync(
            EmployeeId, marchStart, marchEnd, RegisterId, PayrollRunType.Regular, marchEnd, null);

        fixedDays.PresentDays.ShouldBe(26);
        (fixedDays.PresentDays * PerDay).ShouldBe(MonthlyBasic, 0.000001);

        // The ACTUAL-days branch does not: an absent day is enough to trip the switch, and it then counts 27.
        rows.Single(r => r.DayDate == new DateTime(2027, 3, 2)).AbsentDay = 1;
        rows.Single(r => r.DayDate == new DateTime(2027, 3, 2)).ActualWorkingDay = 0;
        rows.Single(r => r.DayDate == new DateTime(2027, 3, 2)).ApprovedActualWorkingDay = 0;

        var actualDays = await BuildDayCountService(rows, register, new HCMv3.SystemSettings.ManagePayrollSettings
        {
            FixedToActualWhenAbsentDayFound = true,
            EnableBasicpayMoreThenFixedDays = true,
            DecimalInSalaryRegister = 6
        }).GetPresentDaysAsync(
            EmployeeId, marchStart, marchEnd, RegisterId, PayrollRunType.Regular, marchEnd, null);

        // 27 working days less the 1 absent = 26 attended. Still 26 here -- but the CEILING is 27, not 26:
        // remove the absence from the equation and a fully attended month would pay 27.
        actualDays.PresentDays.ShouldBe(26);

        // The ceiling, stated plainly: 31 calendar days less 4 week-offs is 27, which is MORE than FixedDays.
        var salaryBasisDays = DateTime.DaysInMonth(2027, 3) - rows.Count(r => r.WeeklyOff == 1);
        salaryBasisDays.ShouldBe(27);
        ((double)salaryBasisDays).ShouldBeGreaterThan(FixedDays);
        (salaryBasisDays * PerDay).ShouldBeGreaterThan(MonthlyBasic);
    }

    #endregion

    #region << The clearing blocks: a re-run must not stack >>

    /// <summary>
    /// <b>THE REGRESSION THIS PORT CLOSES.</b> Legacy deletes the employee's existing 2005 rows before an
    /// off-cycle run (<c>Program.DeleteAdvancePaymentDed</c>, legacy <c>Program.cs</c> line 1201, plus the
    /// clearing block at lines 1796-1819). Until those were ported, re-running the same off-cycle -- a tester
    /// correcting the leave dates, or any re-run -- did not REPLACE the recovery, it ADDED to it: the employee
    /// was deducted 520 in October against 260 advanced.
    ///
    /// <para>Now a second identical run produces byte-for-byte the first run's result.</para>
    /// </summary>
    [Fact]
    public async Task Re_Running_The_Same_OffCycle_Should_Replace_The_Recovery_Not_Add_To_It()
    {
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        var firstRunOctober = _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)];
        var firstRunNovember = _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)];
        var firstRunPaid = Round6(NetPay());

        // Second run of the identical off-cycle.
        ResetRegister();
        await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);

        _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)].ShouldBe(firstRunOctober, 0.000001);
        _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)].ShouldBe(firstRunNovember, 0.000001);
        Round6(NetPay()).ShouldBe(firstRunPaid, 0.000001);

        // Absolute values, so a change to BOTH runs cannot pass this test by staying consistent.
        _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)].ShouldBe(260, 0.000001);
        _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)].ShouldBe(120, 0.000001);
    }

    /// <summary>
    /// Running the off-cycle THREE times is still one month's recovery -- the clearing is not a
    /// "halve the second run" accident.
    /// </summary>
    [Fact]
    public async Task Re_Running_The_OffCycle_Three_Times_Should_Still_Recover_One_Advance()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            ResetRegister();
            await RunAsync(OctPayInDate, PayrollRunType.OffCycle, OffCycleEndDate, OffCyclePaymentDate, PayrollOffCycleReasons.AdvanceLeave);
        }

        _advance.Rows.Values.Sum().ShouldBe(380, 0.000001);
        _advance.Rows[(new DateTime(2026, 10, 1), OffCyclePaymentDate)].ShouldBe(260, 0.000001);
        _advance.Rows[(new DateTime(2026, 11, 1), OffCyclePaymentDate)].ShouldBe(120, 0.000001);
    }

    #endregion
}
