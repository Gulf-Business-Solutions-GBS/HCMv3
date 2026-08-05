using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- <see cref="EmployeePaymentCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeePaymentService.cs</c> (<c>Main</c> line 14,
/// <c>GetAmountFromUnitType</c> line 310).
/// </summary>
public class EmployeePaymentCalculationServiceTests
{
    private const int EmployeeId = 41;
    private const int RegisterId = 600;
    private const int WageTypeId = 7;
    private const int SubGroupId = 9;

    private static readonly DateTime PayDate = new DateTime(2025, 4, 30);

    #region << Harness >>

    private sealed class Harness
    {
        public readonly FakePayrollRegisterStore Store = new FakePayrollRegisterStore();
        public readonly List<EmployeeRecurringPayment> RecurringPayments = new List<EmployeeRecurringPayment>();
        public readonly List<EmployeeSubGroupPayment> SubGroupPayments = new List<EmployeeSubGroupPayment>();
        public readonly List<EmployeeBasicPay> BasicPays = new List<EmployeeBasicPay>();
        public readonly List<EmployeeBasicPayDetail> BasicPayDetails = new List<EmployeeBasicPayDetail>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly IPayrollAdvancePaymentService AdvancePaymentService = Substitute.For<IPayrollAdvancePaymentService>();
        public readonly IPayrollWeekOffCalculator WeekOffCalculator = Substitute.For<IPayrollWeekOffCalculator>();

        public readonly List<(DateTime PaymentDate, double Amount)> AdvancePostings = new List<(DateTime, double)>();

        public Harness()
        {
            AdvancePaymentService
                .GetRecoveryPeriods(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(ci => EmployeeBasicPayCalculationService.GetRecoveryPeriods(
                    ci.ArgAt<DateTime>(0), ci.ArgAt<DateTime>(1)));

            AdvancePaymentService
                .AdvancePaymentDedAsync(
                    Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<double>(),
                    Arg.Any<PayrollRunType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PayrollEngineSettings>())
                .Returns(ci =>
                {
                    AdvancePostings.Add((ci.ArgAt<DateTime>(1), ci.ArgAt<double>(3)));
                    return Task.CompletedTask;
                });

            WeekOffCalculator
                .GetActualWeekOffsAsync(Arg.Any<PayrollCalculationContext>(), Arg.Any<DateTime>())
                .Returns(Task.FromResult(0d));
        }

        public EmployeePaymentCalculationService Build()
        {
            return new EmployeePaymentCalculationService(
                Repo(RecurringPayments),
                Repo(SubGroupPayments),
                Repo(BasicPays),
                Repo(BasicPayDetails),
                Repo(WageTypes),
                Store,
                AdvancePaymentService,
                WeekOffCalculator);
        }
    }

    private static WageType PaymentWageType(
        BillingType billingType = BillingType.Earning,
        BillingMethod billingMethod = BillingMethod.FixedDaysPerMonth,
        double fixedDays = 30)
    {
        return new WageType
        {
            Id = WageTypeId,
            Code = "1400",
            NameEn = "Special Payment",
            BillingType = billingType,
            BillingMethod = billingMethod,
            FixedDays = fixedDays
        };
    }

    private static PayrollCalculationContext Context(WageType wageType, params EmployeePayment[] payments)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            PayDate = PayDate,
            PayStartDate = new DateTime(2025, 4, 1),
            PayInDate = PayDate,
            OffCycleEndDate = PayDate,
            PayrollRunType = PayrollRunType.Regular,
            WageType = wageType,
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayDate,
                InPeriod = PayDate,
                PayrollRunType = PayrollRunType.Regular
            },
            PaymentList = payments.ToList(),
            OrgAssignments = new List<EmployeeOrgAssignment>
            {
                new EmployeeOrgAssignment
                {
                    Id = 1,
                    EmployeeId = EmployeeId,
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2030, 12, 31),
                    EmployeeSubGroupId = SubGroupId
                }
            },
            Settings = new PayrollEngineSettings { DefaultWorkingHours = 8 }
        };
    }

    private static EmployeePayment Payment(int id, double amount, DateTime paymentDate, UnitType unitType = UnitType.Amount)
    {
        return new EmployeePayment
        {
            Id = id,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            Amount = amount,
            PaymentDate = paymentDate,
            UnitType = unitType
        };
    }

    private static void SeedBasicPay(Harness harness, double amount, PayInterval payInterval = PayInterval.Monthly)
    {
        harness.BasicPays.Add(new EmployeeBasicPay
        {
            Id = 1,
            EmployeeId = EmployeeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2030, 12, 31),
            PayInterval = payInterval
        });

        harness.WageTypes.Add(new WageType { Id = 1, Code = "1000", IncludeInPartialPay = true });
        harness.BasicPayDetails.Add(new EmployeeBasicPayDetail
        {
            Id = 1,
            EmployeeBasicPayId = 1,
            WageTypeId = 1,
            Amount = amount
        });
    }

    #endregion

    /// <summary>Legacy lines 70-74: <see cref="UnitType.Amount"/> rows are summed straight in.</summary>
    [Fact]
    public async Task Should_Sum_Amount_Unit_Payments_In_The_Period()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 100, new DateTime(2025, 4, 5)),
            Payment(2, 250, new DateTime(2025, 4, 20))));

        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Amount.ShouldBe(350);
        line.Rate.ShouldBe(0);
        line.Number.ShouldBe(0);
    }

    /// <summary>
    /// Legacy line 185: the write is <c>Amount +=</c>, so an existing line's amount is ADDED TO, not
    /// replaced -- while Rate and Number are hard-zeroed (legacy lines 183-184).
    /// </summary>
    [Fact]
    public async Task Should_Accumulate_Onto_An_Existing_Line_And_Zero_Its_Rate_And_Number()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 55,
            WageTypeId = WageTypeId,
            WageCode = "1400",
            Rate = 12,
            Number = 3,
            Amount = 40
        });

        await harness.Build().CalculateAsync(Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5))));

        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Amount.ShouldBe(140);
        line.Rate.ShouldBe(0);
        line.Number.ShouldBe(0);
    }

    /// <summary>Legacy line 23: <c>IsNotReturnFromLeave</c> suppresses the whole rule.</summary>
    [Fact]
    public async Task Should_Do_Nothing_When_IsNotReturnFromLeave()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        var context = Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5)));
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 70-74: payments outside the month window are excluded.</summary>
    [Fact]
    public async Task Should_Ignore_Payments_Outside_The_Period()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 100, new DateTime(2025, 4, 5)),
            Payment(2, 999, new DateTime(2025, 3, 5))));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(100);
    }

    /// <summary>
    /// Legacy lines 322-332 + 366-368: a <see cref="UnitType.Day"/> payment is converted with
    /// <c>perDaySalary = payRate / FixedDays</c>.
    /// </summary>
    [Fact]
    public async Task Should_Convert_Day_Unit_Payments_Using_FixedDays()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(billingMethod: BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPay(harness, 900);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 2, new DateTime(2025, 4, 5), UnitType.Day)));

        // perDaySalary = 900 / 30 = 30 ; totalDays = 2 -> 60
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(60);
    }

    /// <summary>
    /// Legacy lines 330-331: an <see cref="UnitType.Hour"/> payment is divided by
    /// <c>DefaultWorkingHours</c> before being treated as days.
    /// </summary>
    [Fact]
    public async Task Should_Convert_Hour_Unit_Payments_Using_Default_Working_Hours()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(billingMethod: BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPay(harness, 900);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 16, new DateTime(2025, 4, 5), UnitType.Hour)));

        // totalDays = 16 / 8 = 2 ; perDaySalary = 30 -> 60
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(60);
    }

    /// <summary>Legacy line 347-350: CalendarDaysPerMonth divides by the real day count of the month.</summary>
    [Fact]
    public async Task Should_Convert_Day_Unit_Payments_Using_Calendar_Days()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(billingMethod: BillingMethod.CalendarDaysPerMonth);
        harness.WageTypes.Add(wageType);
        SeedBasicPay(harness, 900);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 1, new DateTime(2025, 4, 5), UnitType.Day)));

        // April = 30 days -> 900 / 30 = 30
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(30);
    }

    /// <summary>Legacy lines 379-382: a DAILY basic pay uses the pay rate directly as the per-day salary.</summary>
    [Fact]
    public async Task Should_Use_The_Pay_Rate_Directly_When_Basic_Pay_Is_Daily()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        SeedBasicPay(harness, 25, PayInterval.Daily);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 3, new DateTime(2025, 4, 5), UnitType.Day)));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(75);
    }

    /// <summary>
    /// <b>Legacy quirk, ported as-is</b> (legacy lines 373-377):
    /// <see cref="BillingMethod.FixedAmountPerMonth"/> is an EMPTY branch, so Day/Hour unit payments on such a
    /// wage type pay nothing at all.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Nothing_For_Day_Unit_Payments_On_A_FixedAmountPerMonth_Wage_Type_Legacy_Quirk()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(billingMethod: BillingMethod.FixedAmountPerMonth);
        harness.WageTypes.Add(wageType);
        SeedBasicPay(harness, 900);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 5, new DateTime(2025, 4, 5), UnitType.Day)));

        harness.Store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// <b>Legacy quirk, ported as-is</b> (legacy line 342): with no basic-pay row the conversion returns 0
    /// even though a Day-unit payment exists.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Nothing_For_Day_Unit_Payments_When_The_Employee_Has_No_Basic_Pay()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        await harness.Build().CalculateAsync(Context(
            wageType,
            Payment(1, 5, new DateTime(2025, 4, 5), UnitType.Day)));

        harness.Store.Lines.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 231-233: a Monthly recurring payment overlapping the month is paid.</summary>
    [Fact]
    public async Task Should_Pay_A_Monthly_Recurring_Payment_Overlapping_The_Period()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.RecurringPayments.Add(new EmployeeRecurringPayment
        {
            Id = 1,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Monthly,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            Amount = 75
        });

        await harness.Build().CalculateAsync(Context(wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(75);
    }

    /// <summary>
    /// <b>Legacy quirk, ported as-is</b> (legacy <c>EmployeePaymentService.cs</c> line 239): inside the month
    /// loop a YEARLY recurring payment matches on the MONTH NUMBER only --
    /// <c>r.StartDate.Month == monthStartdate.Month</c> -- with no year comparison and none of the
    /// <c>!(r.StartDate &gt; monthEnddate || r.EndDate &lt; monthStartdate)</c> range check that the Monthly
    /// branch immediately above it applies. So it pays the FULL amount for a matching month regardless of how
    /// much of that month the payment's own range actually covers.
    ///
    /// <para><b>This test previously over-claimed and was corrected.</b> It asserted that a yearly payment
    /// pays "in that calendar month forever, including before its own StartDate", using a payment starting in
    /// 2099. That is not reachable: the per-employee LOAD predicate (legacy <c>Program.cs</c> line 1880-1882,
    /// ported verbatim) already filters the list with
    /// <c>!(r.StartDate &gt; offCycleEndDate || r.EndDate &lt; payStartDate)</c>, so a payment starting far in
    /// the future never enters <c>empRecPaymentList</c> in the first place -- in legacy either. The quirk is
    /// real but bounded by that outer filter, and the assertion is re-pointed at what legacy actually
    /// does.</para>
    /// </summary>
    [Fact]
    public async Task Should_Pay_A_Yearly_Recurring_Payment_In_Full_On_Month_Number_Match_Legacy_Quirk()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.RecurringPayments.Add(new EmployeeRecurringPayment
        {
            Id = 1,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Yearly,
            // April is the run month, and StartDate.Month == 4 matches. The range deliberately covers only a
            // few days of April: the Yearly branch does no proration and no containment check, so the full
            // 500 is paid rather than a part-month share.
            StartDate = new DateTime(2025, 4, 5),
            EndDate = new DateTime(2025, 4, 8),
            Amount = 500
        });

        await harness.Build().CalculateAsync(Context(wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(500);
    }

    /// <summary>
    /// The bound on the quirk above: a recurring payment whose range lies entirely outside the run window is
    /// excluded by the per-employee LOAD predicate (legacy <c>Program.cs</c> lines 1880-1882) before the
    /// month-number match is ever evaluated -- so no line is written at all. Pinned so that the outer filter
    /// cannot be dropped without a failure here.
    /// </summary>
    [Fact]
    public async Task Should_Not_Load_A_Yearly_Recurring_Payment_Whose_Range_Is_Outside_The_Run_Window()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.RecurringPayments.Add(new EmployeeRecurringPayment
        {
            Id = 1,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Yearly,
            // Month number still matches April, but the whole range is far in the future.
            StartDate = new DateTime(2099, 4, 1),
            EndDate = new DateTime(2099, 12, 31),
            Amount = 500
        });

        await harness.Build().CalculateAsync(Context(wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// Legacy lines 172-200 then 272-299: payments and recurring payments are written in TWO separate
    /// accumulations onto the SAME line, so the result is their sum.
    /// </summary>
    [Fact]
    public async Task Should_Accumulate_Payments_And_Recurring_Payments_Onto_One_Line()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.RecurringPayments.Add(new EmployeeRecurringPayment
        {
            Id = 1,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Monthly,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            Amount = 75
        });

        await harness.Build().CalculateAsync(Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5))));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(175);
    }

    /// <summary>
    /// Legacy lines 83-105: a sub-group payment with <see cref="PayInterval.Yearly"/> pays in the configured
    /// month, matched against the sub-group covering that month's END.
    /// </summary>
    [Fact]
    public async Task Should_Pay_A_Yearly_Sub_Group_Payment_In_Its_Configured_Month()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.SubGroupPayments.Add(new EmployeeSubGroupPayment
        {
            Id = 1,
            EmployeeSubGroupId = SubGroupId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Yearly,
            PayMonth = CustomStartMonth.April,
            Amount = 300
        });

        await harness.Build().CalculateAsync(Context(wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(300);
    }

    /// <summary>Legacy lines 101-105: a different PayMonth pays nothing in this month.</summary>
    [Fact]
    public async Task Should_Not_Pay_A_Yearly_Sub_Group_Payment_Configured_For_Another_Month()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.SubGroupPayments.Add(new EmployeeSubGroupPayment
        {
            Id = 1,
            EmployeeSubGroupId = SubGroupId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.Yearly,
            PayMonth = CustomStartMonth.September,
            Amount = 300
        });

        await harness.Build().CalculateAsync(Context(wageType));

        harness.Store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 142-151: an <see cref="PayInterval.EndOfService"/> sub-group payment is paid ONLY on an
    /// End-Of-Service off-cycle non-retro register.
    /// </summary>
    [Fact]
    public async Task Should_Pay_An_EndOfService_Sub_Group_Payment_Only_On_An_EndOfService_OffCycle_Run()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);
        harness.SubGroupPayments.Add(new EmployeeSubGroupPayment
        {
            Id = 1,
            EmployeeSubGroupId = SubGroupId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.EndOfService,
            Amount = 1000
        });

        // Regular run: nothing.
        await harness.Build().CalculateAsync(Context(wageType));
        harness.Store.Lines.ShouldBeEmpty();

        // End-Of-Service off-cycle: paid.
        var eosHarness = new Harness();
        eosHarness.WageTypes.Add(wageType);
        eosHarness.SubGroupPayments.Add(new EmployeeSubGroupPayment
        {
            Id = 1,
            EmployeeSubGroupId = SubGroupId,
            WageTypeId = WageTypeId,
            PayInterval = PayInterval.EndOfService,
            Amount = 1000
        });

        var context = Context(wageType);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.EndOfService;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await eosHarness.Build().CalculateAsync(context);
        eosHarness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(1000);
    }

    /// <summary>
    /// Legacy lines 112-116: the advance-payment deduction IS sign-flipped for a Deduction wage type
    /// -- unlike <see cref="EmployeeOvertimePayCalculationService"/>, which posts the raw amount.
    /// </summary>
    [Fact]
    public async Task Should_Sign_Flip_The_Advance_Payment_Deduction_For_A_Deduction_Wage_Type()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(BillingType.Deduction);
        wageType.IncludeInAdvancePay = true;
        harness.WageTypes.Add(wageType);

        var context = Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5)));
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await harness.Build().CalculateAsync(context);

        harness.AdvancePostings.Single().Amount.ShouldBe(-100);
    }

    /// <summary>Legacy lines 112-116: an Earning wage type posts the raw positive amount.</summary>
    [Fact]
    public async Task Should_Post_A_Positive_Advance_Payment_Deduction_For_An_Earning_Wage_Type()
    {
        var harness = new Harness();
        var wageType = PaymentWageType(BillingType.Earning);
        wageType.IncludeInAdvancePay = true;
        harness.WageTypes.Add(wageType);

        var context = Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5)));
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await harness.Build().CalculateAsync(context);

        harness.AdvancePostings.Single().Amount.ShouldBe(100);
    }

    /// <summary>
    /// Legacy lines 32-35: an off-cycle run that is neither Advance-Leave-with-IncludeInAdvancePay nor
    /// End-Of-Service does nothing at all.
    /// </summary>
    [Fact]
    public async Task Should_Do_Nothing_On_An_Ineligible_OffCycle_Run()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        wageType.IncludeInAdvancePay = false;
        harness.WageTypes.Add(wageType);

        var context = Context(wageType, Payment(1, 100, new DateTime(2025, 4, 5)));
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// Wave P9 legacy-data regression -- <b>wage code 2028 "Accommodation Rent", employee 3400, June 2024:
    /// 20.00</b>.
    ///
    /// <para>The name is a red herring: 2028 has NOTHING to do with the accommodation module. The string
    /// <c>"2028"</c> appears nowhere in <c>HCM10.PayrollEngine</c> and no accommodation table carries a
    /// WageType column. It is one ordinary <see cref="EmployeeRecurringPayment"/> row
    /// (legacy OID 33: Amount 20.0, StartDate 2017-05-01, EndDate 9999-12-31, PayInterval NULL) that this
    /// service's <c>PayInterval.Monthly</c> branch sums straight in (legacy
    /// <c>HCM10.PayrollEngine/EmployeePaymentService.cs</c> lines 231-233).</para>
    ///
    /// <para><b>PayInterval NULL means Monthly, and that is legacy's own reading, not a migration guess.</b>
    /// Legacy's column is nullable but its XPO property is a NON-nullable <c>PayInterval</c> enum
    /// (<c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeRecurringPayment.cs</c> lines 69-75;
    /// enum in <c>EmployeeBasicPay.cs</c> lines 23-30, <c>Monthly = 0</c>), so XPO materialises NULL as
    /// <c>Monthly</c> -- which is exactly why legacy's own Monthly predicate matches the row.</para>
    ///
    /// <para>The port needed NO engine change for this; the gap was purely that
    /// <c>EmployeeRecurringPayments</c> was missing from <c>MigrationPlan</c>, so the table was empty.</para>
    /// </summary>
    [Fact]
    public async Task Employee_3400_June_2024_Should_Pay_Accommodation_Rent_2028_Of_20()
    {
        var harness = new Harness();

        var accommodationRent = new WageType
        {
            Id = WageTypeId,
            Code = "2028",
            NameEn = "Accommodation Rent",
            BillingType = BillingType.Earning,
            BillingMethod = BillingMethod.FixedDaysPerMonth,
            FixedDays = 30
        };
        harness.WageTypes.Add(accommodationRent);

        harness.RecurringPayments.Add(new EmployeeRecurringPayment
        {
            Id = 1,
            EmployeeId = EmployeeId,
            WageTypeId = WageTypeId,
            // The migrated value of legacy's NULL -- see the summary.
            PayInterval = PayInterval.Monthly,
            StartDate = new DateTime(2017, 5, 1),
            EndDate = new DateTime(9999, 12, 31),
            Amount = 20.0
        });

        await harness.Build().CalculateAsync(Context(accommodationRent));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(20.00);
    }

    /// <summary>Legacy line 41: a payment for a different wage type must not be picked up.</summary>
    [Fact]
    public async Task Should_Ignore_Payments_For_Another_Wage_Type()
    {
        var harness = new Harness();
        var wageType = PaymentWageType();
        harness.WageTypes.Add(wageType);

        var foreign = Payment(1, 500, new DateTime(2025, 4, 5));
        foreign.WageTypeId = 4242;

        await harness.Build().CalculateAsync(Context(wageType, foreign));

        harness.Store.Lines.ShouldBeEmpty();
    }
}
