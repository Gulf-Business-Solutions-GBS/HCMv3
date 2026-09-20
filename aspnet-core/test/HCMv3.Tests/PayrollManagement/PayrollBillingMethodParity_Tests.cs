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
/// <b>LEGACY PARITY ACROSS ALL THREE BILLING METHODS, for BOTH run types.</b> Standing requirement: regular and
/// off-cycle payroll must match the legacy system as is for
/// <see cref="BillingMethod.CalendarDaysPerMonth"/> (calendar days),
/// <see cref="BillingMethod.WorkingDaysPerMonth"/> (calendar minus week-off) and
/// <see cref="BillingMethod.FixedDaysPerMonth"/> (fixed 26 days).
///
/// <para><b>Every expected figure below is the LEGACY FORMULA written out</b>, not a snapshot of what this port
/// happens to produce. That is the whole point: a snapshot test locks in a bug, a formula test catches one. The
/// legacy sources are <c>HCM10.PayrollEngine/EmployeeBasicPayService.cs</c> (regular lines 116-362, off-cycle
/// lines 663-1031) and <c>EmployeeLeavePayService.cs</c> (the billing switch it repeats seven times verbatim).</para>
///
/// <para><b>The fixture</b> is one employee on 260.000 KWD, Friday weekend, advance leave 16/10/2026 - 14/11/2026
/// with <c>IncludeWeekend = false</c> — the same shape as
/// <see cref="OffCycleAdvanceLeaveScenario_Tests"/>, so the two can be read together. October 2026 has 31 days
/// and 5 Fridays, and 13 of its 16 leave calendar days are paid leave days.</para>
///
/// <para><b>Note that WorkingDaysPerMonth and FixedDaysPerMonth coincide in this calendar</b>, because October's
/// salary basis (31 − 5 week-offs) is exactly 26. That is a useful cross-check rather than a weakness: the two
/// formulas are different and are asserted separately, they simply agree on this month.</para>
/// </summary>
public class PayrollBillingMethodParity_Tests
{
    private const int EmployeeId = 1;
    private const int BasicPayWageTypeId = 1000;
    private const int LeavePayWageTypeId = 1050;
    private const int PresentDaysWageTypeId = 4014;
    private const int WeeklyOffWageTypeId = 4008;
    private const int AnnualLeaveAbsenceTypeId = 1;
    private const int RegisterId = 700;

    private const double MonthlyBasic = 260;
    private const double FixedDays = 26;

    private static readonly DateTime OctPayInDate = new DateTime(2026, 10, 31);
    private static readonly DateTime OffCyclePaymentDate = new DateTime(2026, 10, 15);
    private static readonly DateTime LeaveStart = new DateTime(2026, 10, 16);
    private static readonly DateTime LeaveEnd = new DateTime(2026, 11, 14);

    // ---- The facts of this calendar, stated once so the formulas below read like the legacy source. ----
    private const double OctoberCalendarDays = 31;
    private const double OctoberWeekOffs = 5;                       // Fridays 2, 9, 16, 23, 30
    private const double OctoberAttendedDays = 13;                  // 01-15 October less Fridays 2 and 9
    private const double OctoberPaidLeaveDays = 13;                 // 16-31 October less Fridays 16, 23, 30
    private const double NovemberCalendarDays = 30;
    private const double NovemberWeekOffsInWindow = 2;              // Fridays 6, 13 (inside 01-14 November)
    private const double NovemberWeekOffsInMonth = 4;               // Fridays 6, 13, 20, 27
    private const double NovemberPaidLeaveDays = 12;                // 01-14 November less Fridays 6 and 13

    #region << Harness >>

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
                existing = new PayrollRegisterLine { Id = _nextId++, WageTypeId = line.WageTypeId, WageCode = line.WageCode };
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

    /// <summary>Records the wage-code-2005 postings; the clearing operations are not exercised here.</summary>
    private sealed class RecordingAdvancePaymentService : IPayrollAdvancePaymentService
    {
        public Dictionary<DateTime, double> Rows { get; } = new Dictionary<DateTime, double>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            if (amount == 0)
            {
                return Task.CompletedTask;
            }

            Rows.TryGetValue(paymentDate, out var running);
            Rows[paymentDate] = Math.Round(running + amount, settings?.DecimalInSalaryRegister ?? 6);
            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
            => EmployeeBasicPayCalculationService.GetRecoveryPeriods(payStartDate, payEndDate);

        public Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate) => Task.FromResult(0);

        public Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate) => Task.FromResult(0);

        public Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate) => Task.FromResult(0);

        public Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate) => Task.FromResult(0);
    }

    private readonly RegisterStoreFake _store = new RegisterStoreFake();
    private readonly RecordingAdvancePaymentService _advance = new RecordingAdvancePaymentService();

    private static WageType BasicPay(BillingMethod billingMethod) => new WageType
    {
        Id = BasicPayWageTypeId,
        TenantId = 1,
        Code = "1000",
        NameEn = "Basic Pay",
        BillingType = BillingType.Earning,
        BillingMethod = billingMethod,
        FixedDays = FixedDays,
        ProrateMethod = ProrateMethod.WorkingDays,
        IncludeInBasicPay = true,
        IncludeInAdvancePay = true,
        Status = true
    };

    private static WageType LeavePay(BillingMethod billingMethod) => new WageType
    {
        Id = LeavePayWageTypeId,
        TenantId = 1,
        Code = "1050",
        NameEn = "Annual Leave Pay",
        BillingType = BillingType.Earning,
        BillingMethod = billingMethod,
        FixedDays = FixedDays,
        IncludeInAdvancePay = true,
        Status = true
    };

    /// <summary>
    /// Wage code 4014. Its OWN billing method gates <c>GetPresentDaysAsync</c> — the service returns 0 unless it
    /// is <see cref="BillingMethod.FixedDaysPerMonth"/>, which is why the fixed-days path is the only one that
    /// consults it.
    /// </summary>
    private static WageType PresentDays() => new WageType
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

    private static WageType WeeklyOff() => new WageType
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
        PaymentDate = OffCyclePaymentDate
    };

    private static EmployeeContract Contract() => new EmployeeContract
    {
        Id = 1,
        TenantId = 1,
        EmployeeId = EmployeeId,
        StartDate = new DateTime(2020, 1, 1),
        EndDate = new DateTime(2099, 12, 31)
    };

    private static List<TimeRegister> TimeRegisters()
    {
        var rows = new List<TimeRegister>();
        var id = 1;

        for (var date = new DateTime(2026, 9, 1); date <= new DateTime(2026, 11, 30); date = date.AddDays(1))
        {
            var isWeeklyOff = date.DayOfWeek == DayOfWeek.Friday;
            var isInLeave = date >= LeaveStart && date <= LeaveEnd;
            var isLeaveDay = isInLeave && !isWeeklyOff;      // IncludeWeekend = false
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

    private static SalaryRegister Register(PayrollRunType runType) => new SalaryRegister
    {
        Id = RegisterId,
        TenantId = 1,
        EmployeeId = EmployeeId,
        ForPeriod = OctPayInDate,
        InPeriod = OctPayInDate,
        PayrollRunType = runType,
        PayrollResultType = PayrollResultType.Current
    };

    private static IPayrollDayCountService DayCounts(List<TimeRegister> timeRegisters, SalaryRegister register)
        => new PayrollDayCountService(
            Repo(timeRegisters),
            Repo(PresentDays(), WeeklyOff()),
            Repo(AnnualLeave()),
            Repo(register),
            Repo(Enumerable.Empty<SalaryRegisterDetail>()),
            Repo(Enumerable.Empty<PayrollArea>()),
            Repo(Contract()),
            Repo(AdvanceLeave()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(Enumerable.Empty<EmployeeTimePolicy>()),
            Substitute.For<ISysSettingsAccessor>(),
            Substitute.For<IRosterService>());

    private EmployeeBasicPayCalculationService BuildBasicPay(List<TimeRegister> timeRegisters, SalaryRegister register)
        => new EmployeeBasicPayCalculationService(
            Repo(new EmployeeBasicPay
            {
                Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
                PayInterval = PayInterval.Monthly
            }),
            Repo(new EmployeeBasicPayDetail
            {
                Id = 1, TenantId = 1, EmployeeBasicPayId = 1,
                WageTypeId = BasicPayWageTypeId, Amount = MonthlyBasic
            }),
            Repo(Enumerable.Empty<EmployeeRemuneration>()),
            Repo(Enumerable.Empty<EmployeeSeparation>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(timeRegisters),
            _store,
            DayCounts(timeRegisters, register),
            Substitute.For<ITimeDataService>(),
            Substitute.For<IRosterService>(),
            _advance);

    private EmployeeLeavePayCalculationService BuildLeavePay(
        List<TimeRegister> timeRegisters, SalaryRegister register, BillingMethod billingMethod)
    {
        var subGroupResolver = Substitute.For<IEmployeeSubGroupResolver>();
        subGroupResolver.GetEmployeeSubGroupIdAsync(default, default).ReturnsForAnyArgs(Task.FromResult((int?)null));

        return new EmployeeLeavePayCalculationService(
            Repo(AnnualLeave()),
            Repo(BasicPay(billingMethod), LeavePay(billingMethod), PresentDays(), WeeklyOff()),
            Repo(new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId }),
            Repo(AdvanceLeave()),
            Repo(Enumerable.Empty<EmployeeAbsAdjustment>()),
            Repo(Enumerable.Empty<EmployeeSubGroupAbsence>()),
            Repo(Enumerable.Empty<AbsenceDedRuleDetail>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(timeRegisters),
            _store,
            DayCounts(timeRegisters, register),
            Substitute.For<IAccrualService>(),
            Substitute.For<IRosterService>(),
            subGroupResolver,
            _advance);
    }

    private static PayrollCalculationContext Context(
        WageType wageType, PayrollRunType runType, List<TimeRegister> timeRegisters, bool offCycle)
    {
        var payStartDate = new DateTime(2026, 10, 1);

        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = new Employee { Id = EmployeeId },
            PayDate = OctPayInDate,
            PayStartDate = payStartDate,
            PayInDate = OctPayInDate,
            OffCycleEndDate = offCycle ? LeaveEnd : OctPayInDate,
            OffCyclePaymentDate = offCycle ? OffCyclePaymentDate : default(DateTime),
            OffCycleReason = offCycle ? PayrollOffCycleReasons.AdvanceLeave : null,
            PayrollRunType = runType,
            WageType = wageType,
            TimeRegisterList = runType == PayrollRunType.Regular
                ? timeRegisters.Where(r => r.DayDate >= payStartDate && r.DayDate <= OctPayInDate).ToList()
                : null,
            TimeRegisterQuery = timeRegisters.AsQueryable(),
            Settings = new PayrollEngineSettings { DecimalInSalaryRegister = 6 },
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = OctPayInDate,
                InPeriod = OctPayInDate,
                PayrollRunType = runType
            }
        };
    }

    /// <summary>
    /// Runs October for one billing method. On a REGULAR fixed-days run the 4014 "Present Days" line must exist
    /// first — basic pay reads it rather than computing it — so the time-type pre-pass is reproduced with the same
    /// call the real one makes.
    /// </summary>
    private async Task RunOctoberAsync(BillingMethod billingMethod, bool offCycle)
    {
        var timeRegisters = TimeRegisters();
        var runType = offCycle ? PayrollRunType.OffCycle : PayrollRunType.Regular;
        var register = Register(runType);

        if (!offCycle && billingMethod == BillingMethod.FixedDaysPerMonth)
        {
            var presentDays = await DayCounts(timeRegisters, register).GetPresentDaysAsync(
                EmployeeId, new DateTime(2026, 10, 1), OctPayInDate, RegisterId,
                PayrollRunType.Regular, OctPayInDate, null);

            await _store.SaveLineAsync(
                new PayrollRegisterRef { RegisterId = RegisterId, Target = PayrollRegisterTarget.Live },
                new PayrollRegisterLine { WageTypeId = PresentDaysWageTypeId, WageCode = "4014", Number = presentDays.PresentDays },
                deleteWhenAmountIsZero: false);
        }

        await BuildBasicPay(timeRegisters, register).CalculateAsync(
            Context(BasicPay(billingMethod), runType, timeRegisters, offCycle));

        await BuildLeavePay(timeRegisters, register, billingMethod).CalculateAsync(
            Context(LeavePay(billingMethod), runType, timeRegisters, offCycle));
    }

    private PayrollRegisterLine Line(int wageTypeId) => _store.Lines.Single(l => l.WageTypeId == wageTypeId);

    private double Basic => Line(BasicPayWageTypeId).Amount;

    private double Leave => Line(LeavePayWageTypeId).Amount;

    #endregion

    #region << Regular October, per billing method >>

    /// <summary>
    /// <b>Calendar days</b> — legacy <c>EmployeeBasicPayService.cs</c> lines 124-229.
    /// <code>
    /// perDaySalary   = rate / DaysInMonth
    /// payWorkingdays = actualWorkingDays + (weekOffs − weekOffsOnLeaveOrAbsent) + phDays + restDays − includeInLeavePay
    /// </code>
    /// The week-offs are added BACK, which is what makes a full month come to the full salary: 13 attended + 5
    /// Fridays = 18 basic days, and the 13 leave days bring the month to all 31.
    /// </summary>
    [Fact]
    public async Task Regular_CalendarDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.CalendarDaysPerMonth, offCycle: false);

        var perDay = MonthlyBasic / OctoberCalendarDays;
        var expectedBasicDays = OctoberAttendedDays + OctoberWeekOffs;          // 13 + 5 = 18

        Line(BasicPayWageTypeId).Number.ShouldBe(expectedBasicDays);
        Basic.ShouldBe(perDay * expectedBasicDays, 0.000001);                   // 150.967742

        Line(LeavePayWageTypeId).Number.ShouldBe(OctoberPaidLeaveDays);
        Leave.ShouldBe(perDay * OctoberPaidLeaveDays, 0.000001);                // 109.032258

        // Every day of October is accounted for exactly once.
        (expectedBasicDays + OctoberPaidLeaveDays).ShouldBe(OctoberCalendarDays);
        (Basic + Leave).ShouldBe(MonthlyBasic, 0.000001);
    }

    /// <summary>
    /// <b>Calendar minus week-off</b> — legacy lines 233-317, <c>ProrateMethod.WorkingDays</c>.
    /// <code>
    /// salaryBasisDays = DaysInMonth − weekOffs
    /// perDaySalary    = rate / salaryBasisDays
    /// payWorkingdays  = actualWorkingDays + phDays                (week-offs NOT added back)
    /// </code>
    /// </summary>
    [Fact]
    public async Task Regular_WorkingDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.WorkingDaysPerMonth, offCycle: false);

        var salaryBasisDays = OctoberCalendarDays - OctoberWeekOffs;            // 31 − 5 = 26
        var perDay = MonthlyBasic / salaryBasisDays;                            // 10.000

        Line(BasicPayWageTypeId).Number.ShouldBe(OctoberAttendedDays);          // 13, no week-offs added back
        Basic.ShouldBe(perDay * OctoberAttendedDays, 0.000001);                 // 130.000

        Line(LeavePayWageTypeId).Number.ShouldBe(OctoberPaidLeaveDays);
        Leave.ShouldBe(perDay * OctoberPaidLeaveDays, 0.000001);                // 130.000

        (OctoberAttendedDays + OctoberPaidLeaveDays).ShouldBe(salaryBasisDays);
        (Basic + Leave).ShouldBe(MonthlyBasic, 0.000001);
    }

    /// <summary>
    /// <b>Fixed 26 days</b> — legacy lines 321-332.
    /// <code>
    /// perDaySalary   = rate / FixedDays
    /// payWorkingdays = the 4014 "Present Days" line  ( = FixedDays − leaveDays, from GetPresentDays )
    /// </code>
    /// </summary>
    [Fact]
    public async Task Regular_FixedDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.FixedDaysPerMonth, offCycle: false);

        var perDay = MonthlyBasic / FixedDays;                                 // 10.000
        var expectedPresentDays = FixedDays - OctoberPaidLeaveDays;            // 26 − 13 = 13

        Line(PresentDaysWageTypeId).Number.ShouldBe(expectedPresentDays);
        Line(BasicPayWageTypeId).Number.ShouldBe(expectedPresentDays);
        Basic.ShouldBe(perDay * expectedPresentDays, 0.000001);                 // 130.000

        Line(LeavePayWageTypeId).Number.ShouldBe(OctoberPaidLeaveDays);
        Leave.ShouldBe(perDay * OctoberPaidLeaveDays, 0.000001);                // 130.000

        (expectedPresentDays + OctoberPaidLeaveDays).ShouldBe(FixedDays);
        (Basic + Leave).ShouldBe(MonthlyBasic, 0.000001);
    }

    #endregion

    #region << Off-cycle October, per billing method >>

    /// <summary>
    /// <b>Calendar days, off-cycle</b> — legacy lines 671-796. Two recovery months, each at its OWN month's
    /// per-day rate, November clamped to the leave's end date:
    /// <code>
    /// October  : rate/31 × (13 attended + 5 week-offs)      = 150.967742
    /// November : rate/30 × (0 attended + 2 week-offs 01-14) =  17.333333
    /// </code>
    /// November contributes only its week-offs, because legacy adds them back and every other day in the window
    /// is a leave day.
    /// </summary>
    [Fact]
    public async Task OffCycle_CalendarDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.CalendarDaysPerMonth, offCycle: true);

        var octoberBasic = (MonthlyBasic / OctoberCalendarDays) * (OctoberAttendedDays + OctoberWeekOffs);
        var novemberBasic = (MonthlyBasic / NovemberCalendarDays) * NovemberWeekOffsInWindow;

        Basic.ShouldBe(octoberBasic + novemberBasic, 0.000001);                 // 168.301075

        var octoberLeave = (MonthlyBasic / OctoberCalendarDays) * OctoberPaidLeaveDays;
        var novemberLeave = (MonthlyBasic / NovemberCalendarDays) * NovemberPaidLeaveDays;

        Leave.ShouldBe(octoberLeave + novemberLeave, 0.000001);                 // 213.032258

        // October is advanced whole ...
        (octoberBasic + octoberLeave).ShouldBe(MonthlyBasic, 0.000001);

        // ... and the 2005 recovery is split by month, October's share being exactly one month.
        _advance.Rows[new DateTime(2026, 10, 1)].ShouldBe(MonthlyBasic, 0.000001);
        _advance.Rows[new DateTime(2026, 11, 1)].ShouldBe(novemberBasic + novemberLeave, 0.000001);
    }

    /// <summary>
    /// <b>Calendar minus week-off, off-cycle</b> — legacy lines 800-901. November's basic share is 0: week-offs
    /// are NOT added back on this method, and no day in 01-14 November was attended.
    /// </summary>
    [Fact]
    public async Task OffCycle_WorkingDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.WorkingDaysPerMonth, offCycle: true);

        var octoberPerDay = MonthlyBasic / (OctoberCalendarDays - OctoberWeekOffs);          // 260/26
        var novemberPerDay = MonthlyBasic / (NovemberCalendarDays - NovemberWeekOffsInMonth); // 260/26

        Line(BasicPayWageTypeId).Number.ShouldBe(OctoberAttendedDays);          // October only
        Basic.ShouldBe(octoberPerDay * OctoberAttendedDays, 0.000001);          // 130.000

        Leave.ShouldBe(
            (octoberPerDay * OctoberPaidLeaveDays) + (novemberPerDay * NovemberPaidLeaveDays),
            0.000001);                                                         // 130 + 120 = 250.000

        _advance.Rows[new DateTime(2026, 10, 1)].ShouldBe(MonthlyBasic, 0.000001);
        _advance.Rows[new DateTime(2026, 11, 1)].ShouldBe(novemberPerDay * NovemberPaidLeaveDays, 0.000001);
    }

    /// <summary>
    /// <b>Fixed 26 days, off-cycle</b> — legacy lines 905-959. Basic pay asks <c>GetPresentDays</c> per recovery
    /// month, with THIS MONTH'S clamped end date as the bound (legacy line 921 — the argument this port had been
    /// getting wrong). November's month falls past the window end, so it switches to actual days and yields 0.
    /// </summary>
    [Fact]
    public async Task OffCycle_FixedDaysPerMonth_Should_Match_Legacy()
    {
        await RunOctoberAsync(BillingMethod.FixedDaysPerMonth, offCycle: true);

        var perDay = MonthlyBasic / FixedDays;                                 // 10.000
        var octoberPresentDays = FixedDays - OctoberPaidLeaveDays;             // 13

        Line(BasicPayWageTypeId).Number.ShouldBe(octoberPresentDays);          // October only; November is 0
        Basic.ShouldBe(perDay * octoberPresentDays, 0.000001);                 // 130.000

        Leave.ShouldBe(perDay * (OctoberPaidLeaveDays + NovemberPaidLeaveDays), 0.000001);   // 250.000

        _advance.Rows[new DateTime(2026, 10, 1)].ShouldBe(MonthlyBasic, 0.000001);
        _advance.Rows[new DateTime(2026, 11, 1)].ShouldBe(perDay * NovemberPaidLeaveDays, 0.000001);
    }

    #endregion

    #region << Cross-method properties >>

    /// <summary>
    /// <b>October is worth exactly one month on every billing method.</b> The three formulas disagree about how
    /// to split the month between basic and leave pay — 18/13 on calendar days, 13/13 on the other two — but each
    /// splits the SAME month, so basic + leave is 260.000 in all three. This is the single most useful parity
    /// check: it fails the moment a billing method's basic and leave sides stop agreeing on what a day is worth.
    /// </summary>
    [Theory]
    [InlineData(BillingMethod.CalendarDaysPerMonth)]
    [InlineData(BillingMethod.WorkingDaysPerMonth)]
    [InlineData(BillingMethod.FixedDaysPerMonth)]
    public async Task Regular_October_Should_Be_Exactly_One_Month_On_Every_Billing_Method(BillingMethod billingMethod)
    {
        await RunOctoberAsync(billingMethod, offCycle: false);

        (Basic + Leave).ShouldBe(MonthlyBasic, 0.000001);
    }

    /// <summary>
    /// On every billing method the off-cycle advances one whole October plus November's in-window leave, and the
    /// 2005 recovery for October is exactly one month. The November share differs per method, which is correct —
    /// each values November's days by its own rule.
    /// </summary>
    [Theory]
    [InlineData(BillingMethod.CalendarDaysPerMonth)]
    [InlineData(BillingMethod.WorkingDaysPerMonth)]
    [InlineData(BillingMethod.FixedDaysPerMonth)]
    public async Task OffCycle_Should_Recover_Exactly_What_It_Advanced_On_Every_Billing_Method(BillingMethod billingMethod)
    {
        await RunOctoberAsync(billingMethod, offCycle: true);

        // October's recovery is one whole month ...
        _advance.Rows[new DateTime(2026, 10, 1)].ShouldBe(MonthlyBasic, 0.000001);

        // ... November's is less than a month, because only part of it is inside the window ...
        _advance.Rows[new DateTime(2026, 11, 1)].ShouldBeLessThan(MonthlyBasic);

        // ... and together they equal every fils the off-cycle paid out.
        _advance.Rows.Values.Sum().ShouldBe(Math.Round(Basic + Leave, 6), 0.000001);
    }

    #endregion
}
