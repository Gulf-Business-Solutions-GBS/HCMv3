using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P4 -- <see cref="EmployeeBasicPayCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeBasicPayService.Main</c>/<c>GetAmount</c>.
///
/// <para>Pure in-memory: the service uses only synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>
/// (Core has no EF reference), so <see cref="PayrollEngineFakes.Repo{TEntity}(TEntity[])"/> is an exact
/// stand-in. Legacy quirks that are easy to "fix" by accident get their own test.</para>
/// </summary>
public class EmployeeBasicPayCalculationServiceTests
{
    private const int EmployeeId = 7;
    private const int RegisterId = 500;

    private static readonly DateTime PayDate = new DateTime(2025, 3, 31); // 31 calendar days

    #region << Harness >>

    /// <summary>
    /// In-memory <see cref="IPayrollRegisterStore"/>. Reproduces the two behaviours the rules depend on:
    /// legacy's "zero means delete" and the Id assignment on insert. Kept nested (rather than in a shared
    /// helper file) because sibling waves P5/P6 are editing this folder concurrently.
    /// </summary>
    private sealed class PayrollRegisterStoreFake : IPayrollRegisterStore
    {
        private int _nextId = 100;

        public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

        public Dictionary<int, List<SalaryRegisterDayCount>> DayDetails { get; } = new Dictionary<int, List<SalaryRegisterDayCount>>();

        public List<int> DeletedLineIds { get; } = new List<int>();

        public void Seed(PayrollRegisterLine line) => Lines.Add(line);

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
                    DeletedLineIds.Add(existing.Id);
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
                DeletedLineIds.Add(existing.Id);
                Lines.Remove(existing);
            }

            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            Lines.Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
        {
            DayDetails[lineId] = days?.ToList() ?? new List<SalaryRegisterDayCount>();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Records wage-code-2005 postings. Wave P8: the 2005 path used to throw
    /// <c>NotImplementedException</c> here; it now delegates to <see cref="IPayrollAdvancePaymentService"/>,
    /// so the tests assert what is forwarded (including <c>offCyclePaymentDate</c>, the part of legacy's
    /// upsert key whose absence caused the over-deduction risk).
    /// </summary>
    private sealed class RecordingAdvancePaymentService : IPayrollAdvancePaymentService
    {
        public List<(int EmployeeId, DateTime PaymentDate, DateTime OffCyclePaymentDate, double Amount)> Postings { get; }
            = new List<(int, DateTime, DateTime, double)>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            Postings.Add((employeeId, paymentDate, offCyclePaymentDate, amount));
            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
            => new List<DateTime>();
    }

    private readonly RecordingAdvancePaymentService AdvancePaymentService = new RecordingAdvancePaymentService();

    private readonly PayrollRegisterStoreFake _store = new PayrollRegisterStoreFake();

    private EmployeeBasicPayCalculationService Build(
        IEnumerable<EmployeeBasicPay> basicPays = null,
        IEnumerable<EmployeeBasicPayDetail> basicPayDetails = null,
        IEnumerable<EmployeeRemuneration> remunerations = null,
        IEnumerable<EmployeeSeparation> separations = null,
        IEnumerable<TimeRegister> timeRegisters = null,
        IPayrollDayCountService dayCountService = null,
        ITimeDataService timeDataService = null)
    {
        return new EmployeeBasicPayCalculationService(
            Repo(basicPays ?? Enumerable.Empty<EmployeeBasicPay>()),
            Repo(basicPayDetails ?? Enumerable.Empty<EmployeeBasicPayDetail>()),
            Repo(remunerations ?? Enumerable.Empty<EmployeeRemuneration>()),
            Repo(separations ?? Enumerable.Empty<EmployeeSeparation>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(timeRegisters ?? Enumerable.Empty<TimeRegister>()),
            _store,
            dayCountService ?? Substitute.For<IPayrollDayCountService>(),
            timeDataService ?? Substitute.For<ITimeDataService>(),
            Substitute.For<IRosterService>(),
            AdvancePaymentService);
    }

    private static WageType Wage(
        int id,
        string code,
        BillingMethod billingMethod = BillingMethod.CalendarDaysPerMonth,
        ProrateMethod prorateMethod = ProrateMethod.WorkingDays,
        double fixedDays = 26,
        bool includeInBasicPay = true,
        bool includeInAdvancePay = false)
    {
        return new WageType
        {
            Id = id,
            TenantId = 1,
            Code = code,
            NameEn = "Wage " + code,
            BillingType = BillingType.Earning,
            BillingMethod = billingMethod,
            ProrateMethod = prorateMethod,
            FixedDays = fixedDays,
            IncludeInBasicPay = includeInBasicPay,
            IncludeInAdvancePay = includeInAdvancePay,
            Status = true
        };
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        PayrollRunType runType = PayrollRunType.Regular,
        List<TimeRegister> timeRegisterList = null,
        DateTime? forPeriod = null)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = new Employee { Id = EmployeeId },
            PayDate = PayDate,
            PayStartDate = new DateTime(2025, 3, 1),
            PayInDate = PayDate,
            OffCycleEndDate = PayDate,
            PayrollRunType = runType,
            WageType = wageType,
            TimeRegisterList = timeRegisterList,
            TimeRegisterQuery = (timeRegisterList ?? new List<TimeRegister>()).AsQueryable(),
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = forPeriod ?? PayDate,
                InPeriod = PayDate,
                PayrollRunType = runType
            }
        };
    }

    private static EmployeeBasicPay BasicPay(int id = 1)
    {
        return new EmployeeBasicPay
        {
            Id = id,
            TenantId = 1,
            EmployeeId = EmployeeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2099, 12, 31),
            PayInterval = PayInterval.Monthly
        };
    }

    private static EmployeeBasicPayDetail Detail(
        int id,
        int wageTypeId,
        double amount,
        int basicPayId = 1,
        int? dependantWageTypeId = null,
        DependantUnit dependantUnit = DependantUnit.None,
        double dependantNumber = 0)
    {
        return new EmployeeBasicPayDetail
        {
            Id = id,
            TenantId = 1,
            EmployeeBasicPayId = basicPayId,
            WageTypeId = wageTypeId,
            Amount = amount,
            DependantWageTypeId = dependantWageTypeId,
            DependantUnit = dependantUnit,
            DependantNumber = dependantNumber
        };
    }

    private static TimeRegister Day(DateTime date, double actualWorkingDay = 0, double weeklyOff = 0,
        double restDay = 0, double publicHoliday = 0, double leaveDay = 0, int? leaveTypeId = null,
        double includeInLeavePay = 0)
    {
        return new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            DayDate = date,
            ActualWorkingDay = actualWorkingDay,
            WeeklyOff = weeklyOff,
            RestDay = restDay,
            PublicHoliday = publicHoliday,
            LeaveDay = leaveDay,
            LeaveTypeId = leaveTypeId,
            IncludeInLeavePay = includeInLeavePay
        };
    }

    #endregion

    #region << GetBasicPayWageTypeIdsAsync (legacy Program.cs line 1923) >>

    [Fact]
    public async Task GetBasicPayWageTypeIdsAsync_Should_Return_The_Wage_Types_On_The_Covering_BasicPay_Row()
    {
        var service = Build(
            basicPays: new[] { BasicPay() },
            basicPayDetails: new[] { Detail(1, 100, 1000), Detail(2, 101, 500) });

        var ids = await service.GetBasicPayWageTypeIdsAsync(EmployeeId, PayDate);

        ids.ShouldBe(new[] { 100, 101 });
    }

    [Fact]
    public async Task GetBasicPayWageTypeIdsAsync_Should_Be_Empty_When_No_BasicPay_Row_Covers_The_PayDate()
    {
        var basicPay = BasicPay();
        basicPay.EndDate = new DateTime(2024, 12, 31);

        var service = Build(basicPays: new[] { basicPay }, basicPayDetails: new[] { Detail(1, 100, 1000) });

        var ids = await service.GetBasicPayWageTypeIdsAsync(EmployeeId, PayDate);

        ids.ShouldBeEmpty();
    }

    #endregion

    #region << Main (legacy EmployeeBasicPayService.cs line 16) >>

    [Fact]
    public async Task CalculateAsync_Should_Do_Nothing_When_The_WageType_Is_Not_On_The_Employees_BasicPay()
    {
        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 1000) });

        await service.CalculateAsync(Context(Wage(999, "1900")));

        _store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 130-134: when the 1000 Basic Pay line already carries a non-zero Rate, its Number is
    /// reused verbatim as the pay working days -- the whole TimeRegister block is skipped.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Reuse_The_1000_Line_Number_When_Its_Rate_Is_NonZero()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 999, Number = 20 });

        var wage = Wage(100, "1001");
        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 310) });

        await service.CalculateAsync(Context(wage));

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Rate.ShouldBe(310);
        line.Number.ShouldBe(20);
        line.Amount.ShouldBe(310d / 31 * 20); // perDaySalary * payWorkingdays
    }

    /// <summary>
    /// Legacy lines 137-219 with no usable 1000 line: working days come from TimeRegister plus the
    /// weekoff/rest/holiday-minus-leave correction.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Compute_Working_Days_From_TimeRegister_When_No_1000_Line_Exists()
    {
        // 4007 (public holiday days) = 1 register line; one of those PH days also carries a leave day.
        _store.Seed(new PayrollRegisterLine { Id = 2, WageTypeId = 60, WageCode = "4007", Number = 1 });

        var days = new List<TimeRegister>
        {
            Day(new DateTime(2025, 3, 1), actualWorkingDay: 1),
            Day(new DateTime(2025, 3, 2), actualWorkingDay: 1),
            Day(new DateTime(2025, 3, 3), publicHoliday: 1),      // PH, not on leave -> counts
            Day(new DateTime(2025, 3, 4), weeklyOff: 1),          // WOFF, not on leave -> counts
        };

        var service = Build(
            basicPays: new[] { BasicPay() },
            basicPayDetails: new[] { Detail(1, 100, 310) },
            timeRegisters: days);

        await service.CalculateAsync(Context(Wage(100, "1001"), timeRegisterList: days));

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        // actualWorkingDays (2) + PH (1 - 0 on leave) + WOFF (1 - 0) + REST (0) = 4
        line.Number.ShouldBe(4);
        line.Amount.ShouldBe(310d / 31 * 4);
    }

    /// <summary>Legacy lines 152-157: a public holiday that is also a leave day is netted out.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Exclude_Public_Holidays_That_Fall_On_A_Leave_Day()
    {
        _store.Seed(new PayrollRegisterLine { Id = 2, WageTypeId = 60, WageCode = "4007", Number = 1 });

        var days = new List<TimeRegister>
        {
            Day(new DateTime(2025, 3, 3), publicHoliday: 1, leaveDay: 1)
        };

        var service = Build(
            basicPays: new[] { BasicPay() },
            basicPayDetails: new[] { Detail(1, 100, 310) },
            timeRegisters: days);

        await service.CalculateAsync(Context(Wage(100, "1001"), timeRegisterList: days));

        _store.Lines.Single(l => l.WageTypeId == 100).Number.ShouldBe(0);
    }

    /// <summary>Legacy lines 214-219: ActiveDays prorate reads the 4011 line and discards everything else.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Use_The_4011_ActiveDays_Line_When_ProrateMethod_Is_ActiveDays()
    {
        _store.Seed(new PayrollRegisterLine { Id = 3, WageTypeId = 61, WageCode = "4011", Number = 11 });

        var days = new List<TimeRegister> { Day(new DateTime(2025, 3, 1), actualWorkingDay: 1) };

        var service = Build(
            basicPays: new[] { BasicPay() },
            basicPayDetails: new[] { Detail(1, 100, 310) },
            timeRegisters: days);

        var wage = Wage(100, "1001", prorateMethod: ProrateMethod.ActiveDays);
        await service.CalculateAsync(Context(wage, timeRegisterList: days));

        _store.Lines.Single(l => l.WageTypeId == 100).Number.ShouldBe(11);
    }

    /// <summary>Legacy lines 321-332: FixedDaysPerMonth divides by WageType.FixedDays and reads the 4014 line.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Divide_By_FixedDays_And_Read_The_4014_Line_For_FixedDaysPerMonth()
    {
        _store.Seed(new PayrollRegisterLine { Id = 4, WageTypeId = 62, WageCode = "4014", Number = 13 });

        var wage = Wage(100, "1001", billingMethod: BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 260) });

        await service.CalculateAsync(Context(wage));

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Rate.ShouldBe(260);
        line.Number.ShouldBe(13);
        line.Amount.ShouldBe(260d / 26 * 13);
    }

    /// <summary>Legacy lines 336-351: FixedAmountPerMonth pays the rate flat, Number comes from the 1000 line.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Pay_The_Flat_Rate_For_FixedAmountPerMonth()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 999, Number = 30 });

        var wage = Wage(100, "1001", billingMethod: BillingMethod.FixedAmountPerMonth);
        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 75) });

        await service.CalculateAsync(Context(wage));

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Number.ShouldBe(30);
        line.Amount.ShouldBe(75);
    }

    /// <summary>
    /// Legacy lines 223-228: <c>onHold_BasicPay</c> suppresses Number and Amount but the Rate is still
    /// written. This is exactly the divergence <see cref="IWageTypeCalculatorResolver"/>'s generic
    /// <c>IsOnHold</c> gate does NOT cover.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Still_Write_The_Rate_When_BasicPay_Is_OnHold()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 999, Number = 20 });

        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 310) });

        var context = Context(Wage(100, "1001"));
        context.OnHold.BasicPay = true;

        await service.CalculateAsync(context);

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Rate.ShouldBe(310);
        line.Number.ShouldBe(0);
        line.Amount.ShouldBe(0);
    }

    /// <summary>Legacy lines 354-361.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Zero_Number_And_Amount_When_NotReturnFromLeave()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 999, Number = 20 });

        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 310) });

        var context = Context(Wage(100, "1001"));
        context.IsNotReturnFromLeave = true;

        await service.CalculateAsync(context);

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Rate.ShouldBe(310);
        line.Number.ShouldBe(0);
        line.Amount.ShouldBe(0);
    }

    /// <summary>
    /// LEGACY QUIRK (line 81): the dependant branch assigns the computed percentage to <c>Number</c>, not
    /// <c>Amount</c>. Pinned so a future "cleanup" cannot silently change pay.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Write_The_Dependant_Percentage_To_Number_Not_Amount()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 1000, Amount = 800 });

        var detail = Detail(1, 100, 0, dependantWageTypeId: 50,
            dependantUnit: DependantUnit.Percentage, dependantNumber: 25);

        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { detail });

        await service.CalculateAsync(Context(Wage(100, "1002")));

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Number.ShouldBe(200); // 800 * 25 / 100
        line.Amount.ShouldBe(0);
    }

    /// <summary>Legacy lines 77-80: any DependantUnit other than Percentage yields 0.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Yield_Zero_For_A_NonPercentage_Dependant_Unit()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Rate = 1000, Amount = 800 });

        var detail = Detail(1, 100, 0, dependantWageTypeId: 50,
            dependantUnit: DependantUnit.None, dependantNumber: 25);

        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { detail });

        await service.CalculateAsync(Context(Wage(100, "1002")));

        _store.Lines.Single(l => l.WageTypeId == 100).Number.ShouldBe(0);
    }

    /// <summary>
    /// Legacy lines 68-85: on a Regular run a dependant wage type that is NOT IncludeInBasicPay takes the
    /// <c>else</c> arm and its Amount is forced to 0.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Zero_A_Dependant_Line_That_Is_Not_IncludeInBasicPay()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = 50, WageCode = "1000", Amount = 800 });
        _store.Seed(new PayrollRegisterLine { Id = 9, WageTypeId = 100, WageCode = "1002", Amount = 123, Number = 5 });

        var detail = Detail(1, 100, 0, dependantWageTypeId: 50,
            dependantUnit: DependantUnit.Percentage, dependantNumber: 25);

        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { detail });

        await service.CalculateAsync(Context(Wage(100, "1002", includeInBasicPay: false)));

        // Written unconditionally (legacy line 94) even though Amount is 0 -- no "zero means delete" here.
        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Amount.ShouldBe(0);
        _store.DeletedLineIds.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 1026-1027: off-cycle wage types outside the advance/EOS window are zeroed.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Zero_The_Amount_On_An_OffCycle_Run_Outside_The_Advance_Window()
    {
        var service = Build(basicPays: new[] { BasicPay() }, basicPayDetails: new[] { Detail(1, 100, 310) });

        var context = Context(Wage(100, "1001", includeInAdvancePay: false), PayrollRunType.OffCycle);
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await service.CalculateAsync(context);

        var line = _store.Lines.Single(l => l.WageTypeId == 100);
        line.Rate.ShouldBe(310); // legacy line 368 assigns the rate before the gate
        line.Amount.ShouldBe(0);
    }

    #endregion

    #region << GetRecoveryPeriods (legacy Program.cs line 2922) >>

    [Fact]
    public void GetRecoveryPeriods_Should_Return_One_MonthStart_Per_Spanned_Month()
    {
        var periods = EmployeeBasicPayCalculationService.GetRecoveryPeriods(
            new DateTime(2025, 3, 15), new DateTime(2025, 5, 10));

        periods.ShouldBe(new[]
        {
            new DateTime(2025, 3, 1),
            new DateTime(2025, 4, 1),
            new DateTime(2025, 5, 1)
        });
    }

    [Fact]
    public void GetRecoveryPeriods_Should_Return_A_Single_Month_When_The_Window_Closes_In_The_Same_Month()
    {
        var periods = EmployeeBasicPayCalculationService.GetRecoveryPeriods(
            new DateTime(2025, 3, 15), new DateTime(2025, 3, 20));

        periods.ShouldBe(new[] { new DateTime(2025, 3, 1) });
    }

    #endregion
}
