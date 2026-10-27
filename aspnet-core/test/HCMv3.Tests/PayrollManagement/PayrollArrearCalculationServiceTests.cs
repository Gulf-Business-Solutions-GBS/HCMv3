using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- <see cref="PayrollArrearCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/PayrollArrearService.cs</c> (<c>Main</c> line 13, <c>GetArrear</c> line 84).
///
/// <para>Pure in-memory: the service uses only synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>.
/// A MULTI-REGISTER fake store is used (the shared <see cref="FakePayrollRegisterStore"/> ignores the
/// register ref, and this rule's whole job is reading lines off OTHER registers).</para>
/// </summary>
public class PayrollArrearCalculationServiceTests
{
    private const int EmployeeId = 11;
    private const int CurrentRegisterId = 900;

    private static readonly DateTime PayInDate = new DateTime(2025, 6, 30);

    #region << Harness >>

    /// <summary>
    /// Register-aware in-memory store: lines are keyed by <see cref="PayrollRegisterRef.RegisterId"/>, which
    /// is exactly what the arrear rule needs (it walks <c>CurrentSalaryRegisters</c> and the prior regular
    /// register).
    /// </summary>
    private sealed class MultiRegisterStoreFake : IPayrollRegisterStore
    {
        private int _nextId = 1;

        public Dictionary<int, List<PayrollRegisterLine>> ByRegister { get; } = new Dictionary<int, List<PayrollRegisterLine>>();

        public List<int> DeletedLineIds { get; } = new List<int>();

        public void Seed(int registerId, int wageTypeId, double rate = 0, double number = 0, double amount = 0)
        {
            Lines(registerId).Add(new PayrollRegisterLine
            {
                Id = _nextId++,
                WageTypeId = wageTypeId,
                Rate = rate,
                Number = number,
                Amount = amount
            });
        }

        public List<PayrollRegisterLine> Lines(int registerId)
        {
            if (!ByRegister.TryGetValue(registerId, out var lines))
            {
                lines = new List<PayrollRegisterLine>();
                ByRegister[registerId] = lines;
            }

            return lines;
        }

        public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
            => Task.FromResult(Lines(register.RegisterId).ToList());

        public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
            => Task.FromResult(Lines(register.RegisterId).FirstOrDefault(l => l.WageCode == wageCode));

        public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
            => Task.FromResult(Lines(register.RegisterId).FirstOrDefault(l => l.WageTypeId == wageTypeId));

        public Task<PayrollRegisterLine> SaveLineAsync(PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
        {
            var lines = Lines(register.RegisterId);
            var existing = lines.FirstOrDefault(l => l.WageTypeId == line.WageTypeId);

            if (deleteWhenAmountIsZero && line.Amount == 0)
            {
                if (existing != null)
                {
                    DeletedLineIds.Add(existing.Id);
                    lines.Remove(existing);
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
                lines.Add(existing);
            }

            existing.Rate = line.Rate;
            existing.Number = line.Number;
            existing.Amount = line.Amount;

            line.Id = existing.Id;
            return Task.FromResult(line);
        }

        public Task DeleteLineAsync(PayrollRegisterRef register, int wageTypeId)
        {
            var lines = Lines(register.RegisterId);
            var existing = lines.FirstOrDefault(l => l.WageTypeId == wageTypeId);
            if (existing != null)
            {
                DeletedLineIds.Add(existing.Id);
                lines.Remove(existing);
            }

            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            Lines(register.RegisterId).Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
            => Task.CompletedTask;
    }

    private sealed class Harness
    {
        public readonly MultiRegisterStoreFake Store = new MultiRegisterStoreFake();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<SalaryRegister> SalaryRegisters = new List<SalaryRegister>();

        public PayrollArrearCalculationService Build()
        {
            return new PayrollArrearCalculationService(
                Repo(WageTypes),
                Repo(SalaryRegisters),
                Store);
        }
    }

    private static WageType WageType(int id, string code, BillingType billingType = BillingType.Earning, int? arrearSourceId = null)
    {
        return new WageType
        {
            Id = id,
            Code = code,
            NameEn = code,
            BillingType = billingType,
            IncludeInArrearPay = arrearSourceId != null,
            ArrearSourceWageTypeId = arrearSourceId
        };
    }

    private static SalaryRegister Register(int id, DateTime forPeriod, DateTime inPeriod, PayrollRunType runType = PayrollRunType.Regular)
    {
        return new SalaryRegister
        {
            Id = id,
            EmployeeId = EmployeeId,
            ForPeriod = forPeriod,
            InPeriod = inPeriod,
            PayrollRunType = runType,
            PayrollResultType = PayrollResultType.Current
        };
    }

    private static PayrollCalculationContext Context(Harness harness, WageType arrearWageType, params SalaryRegister[] currentRegisters)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            PayDate = PayInDate,
            PayStartDate = new DateTime(2025, 6, 1),
            PayInDate = PayInDate,
            OffCycleEndDate = PayInDate,
            PayrollRunType = PayrollRunType.Regular,
            WageType = arrearWageType,
            Register = new PayrollRegisterRef
            {
                RegisterId = CurrentRegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayInDate,
                InPeriod = PayInDate,
                PayrollRunType = PayrollRunType.Regular
            },
            CurrentSalaryRegisters = currentRegisters.ToList()
        };
    }

    #endregion

    /// <summary>
    /// The core rule (legacy <c>PayrollArrearService.cs</c> lines 106-119): this run's retro register pays
    /// 500 for the source wage type, the previously-paid regular register already paid 300, so the arrear is
    /// the 200 difference.
    /// </summary>
    [Fact]
    public async Task Should_Post_The_Difference_Between_This_Runs_Retro_Register_And_The_Previously_Paid_One()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(priorRegister.Id, source.Id, amount: 300);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        var line = harness.Store.Lines(CurrentRegisterId).Single();
        line.WageTypeId.ShouldBe(arrear.Id);
        line.Amount.ShouldBe(200);
    }

    /// <summary>
    /// Legacy line 111/118: a <see cref="BillingType.TimeType"/> SOURCE wage type is read from
    /// <c>Number</c>, not <c>Amount</c>.
    /// </summary>
    [Fact]
    public async Task Should_Read_Number_Instead_Of_Amount_When_The_Source_Wage_Type_Is_A_TimeType()
    {
        var harness = new Harness();
        var source = WageType(1, "4000", BillingType.TimeType);
        var arrear = WageType(2, "5000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);

        // Amount is deliberately different from Number: only Number must be used.
        harness.Store.Seed(retroRegister.Id, source.Id, number: 7, amount: 999);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(7);
    }

    /// <summary>
    /// Legacy line 108 (with its comment): the <c>continue</c> was deliberately REMOVED, so a retro register
    /// that has NO line for the source wage type still lets the prior register's amount be subtracted --
    /// producing the negative reversal for a payment that was deleted this month.
    /// </summary>
    [Fact]
    public async Task Should_Produce_A_Negative_Reversal_When_The_Retro_Register_Has_No_Source_Line()
    {
        var harness = new Harness();
        var source = WageType(1, "1006");
        var arrear = WageType(2, "7006", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        // Nothing seeded on the retro register for `source`.
        harness.Store.Seed(priorRegister.Id, source.Id, amount: 100);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(-100);
    }

    /// <summary>
    /// Legacy lines 69-75: a ZERO arrear does NOT delete the existing row and does NOT create one -- it
    /// zeroes an existing row's Amount while leaving Rate/Number alone (legacy lines 57-58 are commented out).
    /// </summary>
    [Fact]
    public async Task Should_Zero_An_Existing_Line_Rather_Than_Delete_It_When_The_Arrear_Is_Zero()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 400);
        harness.Store.Seed(priorRegister.Id, source.Id, amount: 400);

        harness.Store.Seed(CurrentRegisterId, arrear.Id, rate: 12, number: 3, amount: 55);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        var line = harness.Store.Lines(CurrentRegisterId).Single();
        line.Amount.ShouldBe(0);
        line.Rate.ShouldBe(12);
        line.Number.ShouldBe(3);
        harness.Store.DeletedLineIds.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 69-75: a zero arrear with no existing row creates nothing.</summary>
    [Fact]
    public async Task Should_Not_Create_A_Line_When_The_Arrear_Is_Zero_And_No_Line_Exists()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 57-58 are COMMENTED OUT: an existing line's Rate and Number survive an arrear write.
    /// </summary>
    [Fact]
    public async Task Should_Leave_Rate_And_Number_Untouched_On_An_Existing_Line()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);
        harness.Store.Seed(retroRegister.Id, source.Id, amount: 250);
        harness.Store.Seed(CurrentRegisterId, arrear.Id, rate: 9, number: 4, amount: 0);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        var line = harness.Store.Lines(CurrentRegisterId).Single();
        line.Amount.ShouldBe(250);
        line.Rate.ShouldBe(9);
        line.Number.ShouldBe(4);
    }

    /// <summary>
    /// Legacy line 22: the <c>IsNotReturnFromLeave</c> arm is EMPTY and it is an <c>else if</c>, so a
    /// non-Provision wage type produces NOTHING at all -- not a zero.
    /// </summary>
    [Fact]
    public async Task Should_Do_Nothing_When_IsNotReturnFromLeave_And_The_Wage_Type_Is_Not_A_Provision()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);
        harness.Store.Seed(retroRegister.Id, source.Id, amount: 250);

        var context = Context(harness, arrear, retroRegister);
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>Legacy line 22: a Provision wage type FALLS THROUGH the empty arm and is still calculated.</summary>
    [Fact]
    public async Task Should_Still_Calculate_When_IsNotReturnFromLeave_And_The_Wage_Type_Is_A_Provision()
    {
        var harness = new Harness();
        var source = WageType(1, "3000", BillingType.Provision);
        var arrear = WageType(2, "8000", BillingType.Provision, arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);
        harness.Store.Seed(retroRegister.Id, source.Id, amount: 60);

        var context = Context(harness, arrear, retroRegister);
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(60);
    }

    /// <summary>Legacy line 29: no retro registers at all means no arrear work.</summary>
    [Fact]
    public async Task Should_Do_Nothing_When_There_Are_No_Retro_Registers()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        await harness.Build().CalculateAsync(Context(harness, arrear));

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy line 31-32: a wage type flagged <c>IncludeInArrearPay</c> but with NO
    /// <c>ArrearSourceWageType</c> is a no-op. This is the case that makes the arm safe to evaluate for every
    /// wage type of every employee.
    /// </summary>
    [Fact]
    public async Task Should_Do_Nothing_When_The_Wage_Type_Has_No_Arrear_Source()
    {
        var harness = new Harness();
        var arrear = WageType(2, "7000");
        arrear.IncludeInArrearPay = true;
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>Legacy lines 32/91: a retro period (<c>payDate != payInDate</c>) posts no arrear.</summary>
    [Fact]
    public async Task Should_Do_Nothing_On_A_Retro_Period_Where_PayDate_Differs_From_PayInDate()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroRegister = Register(901, new DateTime(2025, 5, 31), PayInDate);
        harness.SalaryRegisters.Add(retroRegister);
        harness.Store.Seed(retroRegister.Id, source.Id, amount: 250);

        var context = Context(harness, arrear, retroRegister);
        context.PayDate = new DateTime(2025, 5, 31);

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy line 100: the prior-register predicate is <c>InPeriod != payInDate</c> -- NOT
    /// <c>InPeriod &lt; payInDate</c>, which is what <c>Program.GetArrear</c> (legacy <c>Program.cs</c> line
    /// 5325) uses. A FUTURE-dated regular register for the same ForPeriod therefore IS subtracted here,
    /// where <c>Program.GetArrear</c> would ignore it. Pinned so the two are never accidentally unified.
    /// </summary>
    [Fact]
    public async Task Should_Subtract_A_Future_Dated_Regular_Register_Unlike_Program_GetArrear()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var futureRegister = Register(902, retroForPeriod, new DateTime(2025, 7, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(futureRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(futureRegister.Id, source.Id, amount: 500);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        // 500 - 500 == 0 -> nothing created (legacy's zero branch).
        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy line 96: on an OFF-CYCLE run that is NOT End-Of-Service, the prior register is never looked up
    /// at all -- so nothing is subtracted and the retro register's full amount becomes the arrear.
    /// </summary>
    [Fact]
    public async Task Should_Not_Subtract_The_Prior_Register_On_A_Non_EndOfService_OffCycle_Run()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(priorRegister.Id, source.Id, amount: 300);

        var context = Context(harness, arrear, retroRegister);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(500);
    }

    /// <summary>Legacy line 96: an End-Of-Service off-cycle run DOES look the prior register up.</summary>
    [Fact]
    public async Task Should_Subtract_The_Prior_Register_On_An_EndOfService_OffCycle_Run()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(priorRegister.Id, source.Id, amount: 300);

        var context = Context(harness, arrear, retroRegister);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.EndOfService;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(200);
    }

    /// <summary>
    /// Legacy line 102: only <see cref="PayrollRunType.Regular"/> registers count as "already paid". An
    /// off-cycle register for the same period is ignored.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_A_Prior_OffCycle_Register_When_Subtracting()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorOffCycle = Register(902, retroForPeriod, new DateTime(2025, 5, 31), PayrollRunType.OffCycle);
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorOffCycle);

        harness.Store.Seed(retroRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(priorOffCycle.Id, source.Id, amount: 300);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(500);
    }

    /// <summary>
    /// <b>Regression pin for the wage-code-5022 investigation (wave P9).</b>
    ///
    /// <para>Legacy's <c>WageTypeList</c> is loaded UNFILTERED (legacy <c>Program.cs</c> line 2663) and every
    /// by-CODE / by-REFERENCE wage-type lookup outside the <c>PayrollOrder</c> loop -- <c>Program.GetWageType</c>
    /// (legacy line 2748), <c>Program.CalculateTimeType</c> (legacy line 2717) and
    /// <c>currWageType.ArrearSourceWageType</c> (legacy <c>PayrollArrearService.cs</c> line 31) -- applies NO
    /// <c>Status</c> and NO <c>BillingType</c> test. Only the <c>PayrollOrder</c> loop itself filters (legacy
    /// line 2524). So legacy genuinely reads from, and posts to, INACTIVE wage types.</para>
    ///
    /// <para>Confirmed against production: in <c>HRMS_RCF</c>, wage code <c>5022 Annual Leave Closing Balance
    /// Arrear</c> (<c>Status = 0</c>, <c>BillingType = TimeType</c>, PayrollOrder 10400) carries
    /// <c>Number = 9</c> on employee 4225's June-2024 register. That is legacy behaviour, arguably a defect,
    /// and it is NOT ours to correct.</para>
    ///
    /// <para>This test pins the half of that behaviour which lives in code this wave owns: an INACTIVE,
    /// TimeType arrear SOURCE wage type must still be resolved and read.</para>
    /// </summary>
    [Fact]
    public async Task Should_Still_Resolve_An_Inactive_TimeType_Arrear_Source_Wage_Type()
    {
        var harness = new Harness();

        // Status deliberately false, BillingType deliberately TimeType -- exactly wage code 5022's shape.
        var source = WageType(1, "4022", BillingType.TimeType);
        source.Status = false;
        var arrear = WageType(2, "5022", BillingType.TimeType, arrearSourceId: source.Id);
        arrear.Status = false;
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var retroForPeriod = new DateTime(2025, 5, 31);
        var retroRegister = Register(901, retroForPeriod, PayInDate);
        var priorRegister = Register(902, retroForPeriod, new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(retroRegister);
        harness.SalaryRegisters.Add(priorRegister);

        harness.Store.Seed(retroRegister.Id, source.Id, number: 16);
        harness.Store.Seed(priorRegister.Id, source.Id, number: 7);

        await harness.Build().CalculateAsync(Context(harness, arrear, retroRegister));

        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(9);
    }

    /// <summary>
    /// <b>The actual root cause of the missing 5022 line</b> (wave P9 investigation): when the run produces no
    /// RETRO registers, <c>CurrentSalaryRegisters</c> is empty and every arrear -- both this service's
    /// <c>7000</c>-family and the time-type <c>5xxx</c> family that shares the same precondition -- computes
    /// exactly 0 and writes nothing. Legacy behaves identically (legacy
    /// <c>PayrollArrearService.cs</c> line 29, <c>Program.cs</c> line 5307).
    ///
    /// <para>Pinned so that "no arrear was written" is never again read as evidence of a wage-type FILTER
    /// problem when it is really an absent-retro-period problem.</para>
    /// </summary>
    [Fact]
    public async Task Should_Write_Nothing_At_All_When_There_Are_No_Retro_Registers_Even_Though_Source_Data_Exists()
    {
        var harness = new Harness();
        var source = WageType(1, "4022", BillingType.TimeType);
        var arrear = WageType(2, "5022", BillingType.TimeType, arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        // A fully-populated prior register exists, but it is not a RETRO register of this run.
        var priorRegister = Register(902, new DateTime(2025, 5, 31), new DateTime(2025, 5, 31));
        harness.SalaryRegisters.Add(priorRegister);
        harness.Store.Seed(priorRegister.Id, source.Id, number: 7);

        await harness.Build().CalculateAsync(Context(harness, arrear));

        harness.Store.Lines(CurrentRegisterId).ShouldBeEmpty();
    }

    /// <summary>Legacy lines 93-121: several retro registers accumulate into one arrear figure.</summary>
    [Fact]
    public async Task Should_Accumulate_Across_Every_Retro_Register()
    {
        var harness = new Harness();
        var source = WageType(1, "1000");
        var arrear = WageType(2, "7000", arrearSourceId: source.Id);
        harness.WageTypes.Add(source);
        harness.WageTypes.Add(arrear);

        var aprilRegister = Register(901, new DateTime(2025, 4, 30), PayInDate);
        var mayRegister = Register(902, new DateTime(2025, 5, 31), PayInDate);
        var priorApril = Register(903, new DateTime(2025, 4, 30), new DateTime(2025, 4, 30));
        harness.SalaryRegisters.Add(aprilRegister);
        harness.SalaryRegisters.Add(mayRegister);
        harness.SalaryRegisters.Add(priorApril);

        harness.Store.Seed(aprilRegister.Id, source.Id, amount: 500);
        harness.Store.Seed(mayRegister.Id, source.Id, amount: 700);
        harness.Store.Seed(priorApril.Id, source.Id, amount: 450);

        await harness.Build().CalculateAsync(Context(harness, arrear, aprilRegister, mayRegister));

        // (500 - 450) + 700
        harness.Store.Lines(CurrentRegisterId).Single().Amount.ShouldBe(750);
    }
}
