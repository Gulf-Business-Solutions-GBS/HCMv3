using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P6 -- the statutory / insurance / fund / average / net-pay <see cref="IWageTypeCalculator"/> rules
/// ported from legacy <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c>.
///
/// <para>Coverage priority follows the wave brief: the statutory percentage-and-cap maths (wage codes 1 and
/// 3), the three averages and their period-window selection (27, 29, 31) and 9000 Net Pay. The remaining
/// rules are represented by their gate/shape assertions.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class StatutoryWageTypeCalculators_Tests
{
    private const int RegisterId = 900;
    private const int EmployeeId = 7;

    #region << Fake register store >>

    /// <summary>
    /// In-memory <see cref="IPayrollRegisterStore"/> reproducing the two behaviours the rules depend on:
    /// legacy "zero means delete" and Id assignment on insert. (A private copy rather than a shared helper --
    /// sibling porting waves are editing this folder concurrently.)
    /// </summary>
    private sealed class FakeRegisterStore : IPayrollRegisterStore
    {
        private int _nextId = 1;

        public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

        public List<int> DeletedLineIds { get; } = new List<int>();

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

        public Task ClearLinesAsync(PayrollRegisterRef register) { Lines.Clear(); return Task.CompletedTask; }

        public Task SaveDayDetailsAsync(PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
            => Task.CompletedTask;
    }

    /// <summary>Records every 2005 posting so the sign convention per wage code can be asserted.</summary>
    private sealed class FakeAdvancePaymentService : IPayrollAdvancePaymentService
    {
        public List<(DateTime PaymentDate, double Amount)> Postings { get; } = new List<(DateTime, double)>();

        /// <summary>Wave P8 added <c>offCyclePaymentDate</c> to the upsert key; recorded so it can be asserted.</summary>
        public List<DateTime> OffCyclePaymentDates { get; } = new List<DateTime>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            if (amount != 0)
            {
                Postings.Add((paymentDate, amount));
                OffCyclePaymentDates.Add(offCyclePaymentDate);
            }

            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
        {
            var periods = new List<DateTime>();
            var cursor = new DateTime(payStartDate.Year, payStartDate.Month, 1);
            while (cursor <= payEndDate)
            {
                periods.Add(cursor);
                cursor = cursor.AddMonths(1);
            }

            return periods;
        }

        // The four clearing operations are called by the ENGINE, never by a wage-type calculator, so this
        // double no-ops them. PayrollAdvancePaymentService_Clearing_Tests covers the real behaviour.
        public Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate)
            => Task.FromResult(0);

        public Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate)
            => Task.FromResult(0);

        public Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate)
            => Task.FromResult(0);

        public Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate)
            => Task.FromResult(0);
    }

    #endregion

    #region << Builders >>

    private static WageType Wage(int id, string code, BillingType billingType = BillingType.Deduction)
    {
        return new WageType { Id = id, TenantId = 1, Code = code, NameEn = "Wage " + code, BillingType = billingType, Status = true };
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        PayrollRunType runType = PayrollRunType.Regular,
        string offCycleReason = null,
        DateTime? payDate = null,
        Employee employee = null)
    {
        var pay = payDate ?? new DateTime(2025, 3, 31);

        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = employee ?? new Employee { Id = EmployeeId, TenantId = 1, Code = "E1", NationalityId = 11 },
            PayDate = pay,
            PayStartDate = new DateTime(pay.Year, pay.Month, 1),
            PayInDate = pay,
            PayrollRunType = runType,
            OffCycleReason = offCycleReason,
            WageType = wageType,
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = pay,
                InPeriod = pay,
                PayrollRunType = runType
            }
        };
    }

    #endregion

    #region << 1 / 3 -- PIFSS percentage, min-earnings step and EarningsCap (legacy 3002-3210) >>

    /// <summary>
    /// Builds the wage-code 1/3 dependency graph. One social-security master row for nationality 11 with a
    /// single slab, and an employee social-security row that covers the period.
    /// </summary>
    private static (EmployeePifssDeductionWageTypeCalculator Employee,
                    EmployerPifssDeductionWageTypeCalculator Employer,
                    FakeRegisterStore Store,
                    FakeAdvancePaymentService Advance)
        BuildPifss(double earningsCap, SocialSecurityDetail slab, double registerRate, WageType[] wageTypes)
    {
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();

        var socialSecurity = new SocialSecurity
        {
            Id = 1,
            TenantId = 1,
            NationalityId = 11,
            StartDate = new DateTime(2000, 1, 1),
            EndDate = new DateTime(2099, 12, 31),
            EarningsCap = earningsCap
        };

        slab.Id = 1;
        slab.SocialSecurityId = 1;

        // The rate base: a register line on a wage type flagged IncludeInSocialSecurity.
        store.Lines.Add(new PayrollRegisterLine { Id = 99, WageTypeId = 1000, WageCode = "1000", Rate = registerRate });

        var allWageTypes = wageTypes.Concat(new[]
        {
            new WageType { Id = 1000, TenantId = 1, Code = "1000", NameEn = "Basic", IncludeInSocialSecurity = true }
        }).ToArray();

        EmployeePifssDeductionWageTypeCalculator MakeEmployee() => new EmployeePifssDeductionWageTypeCalculator(
            store, advance,
            Repo(new EmployeeSocialSecurity
            {
                Id = 1, TenantId = 1, EmployeeId = EmployeeId, SocialSecurityContrib = true,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(2099, 12, 31)
            }),
            Repo(socialSecurity), Repo(slab),
            Repo<SocialSecurityMaritalAllowance>(), Repo<SocialSecurityEducationAllowance>(),
            Repo<EmployeeEducation>(), Repo<EmployeeDependant>(), Repo<EmployeeSponsor>(),
            Repo<MaritalStatus>(), Repo(allWageTypes));

        EmployerPifssDeductionWageTypeCalculator MakeEmployer() => new EmployerPifssDeductionWageTypeCalculator(
            store, advance,
            Repo(new EmployeeSocialSecurity
            {
                Id = 1, TenantId = 1, EmployeeId = EmployeeId, SocialSecurityContrib = true,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(2099, 12, 31)
            }),
            Repo(socialSecurity), Repo(slab),
            Repo<SocialSecurityMaritalAllowance>(), Repo<SocialSecurityEducationAllowance>(),
            Repo<EmployeeEducation>(), Repo<EmployeeDependant>(), Repo<EmployeeSponsor>(),
            Repo<MaritalStatus>(), Repo(allWageTypes));

        return (MakeEmployee(), MakeEmployer(), store, advance);
    }

    private static PayrollCalculationContext PifssContext(WageType wageType)
    {
        var context = Context(wageType);
        context.Settings = new PayrollEngineSettings
        {
            SocialSecurityRateRule = SocialSecurityRateRule.WageSalary,
            DecimalInSalaryRegister = 6
        };
        return context;
    }

    [Fact]
    public async Task EmployeePifss_Should_Add_The_MinEarnings_Step_And_The_Percentage_Of_The_Rate()
    {
        // Legacy lines 3140-3144: (MinEarnings - 1) * MinEarningsEmployeeContribution / 100
        //                        + rate * EmployeeContribution / 100
        var wageType = Wage(1, "1");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 231,
            MaxEarnings = 2750,
            EmployeeContribution = 2.5,
            EmployerContribution = 11.5,
            MinEarningsEmployeeContribution = 5
        };

        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });

        await built.Employee.CalculateAsync(PifssContext(wageType));

        var expected = ((231 - 1) * 5 / 100.0) + (1000 * 2.5 / 100.0);
        built.Store.Lines.Single(l => l.WageTypeId == 1).Amount.ShouldBe(expected, 1e-9);
    }

    [Fact]
    public async Task EmployerPifss_Should_Use_The_EmployerContribution_Percentage_And_Skip_The_MinEarnings_Step()
    {
        var wageType = Wage(3, "3");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 231,
            MaxEarnings = 2750,
            EmployeeContribution = 2.5,
            EmployerContribution = 11.5,
            MinEarningsEmployeeContribution = 5
        };

        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });

        await built.Employer.CalculateAsync(PifssContext(wageType));

        built.Store.Lines.Single(l => l.WageTypeId == 3).Amount.ShouldBe(1000 * 11.5 / 100.0, 1e-9);
    }

    [Fact]
    public async Task EmployerPifss_Should_Pay_Nothing_When_EmployeeContribution_Is_Zero()
    {
        // PRESERVED LEGACY QUIRK (legacy line 3152): the employer arm is guarded by EmployeeContribution but
        // multiplies by EmployerContribution. A slab with 0 employee contribution therefore yields no
        // EMPLOYER contribution either.
        var wageType = Wage(3, "3");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 0,
            MaxEarnings = 2750,
            EmployeeContribution = 0,
            EmployerContribution = 11.5
        };

        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });

        await built.Employer.CalculateAsync(PifssContext(wageType));

        built.Store.Lines.Any(l => l.WageTypeId == 3).ShouldBeFalse();
    }

    [Fact]
    public async Task Pifss_Should_Clamp_The_Contribution_To_EarningsCap()
    {
        // Legacy lines 3160-3161: the cap is applied to the CONTRIBUTION, not to the earnings.
        var wageType = Wage(1, "1");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 0,
            MaxEarnings = 999999,
            EmployeeContribution = 50,
            EmployerContribution = 0,
            MinEarningsEmployeeContribution = 0
        };

        var built = BuildPifss(earningsCap: 12.5, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });

        await built.Employee.CalculateAsync(PifssContext(wageType));

        built.Store.Lines.Single(l => l.WageTypeId == 1).Amount.ShouldBe(12.5);
    }

    [Fact]
    public async Task Pifss_Should_Fall_Back_To_The_Top_Slab_And_Replace_The_Rate_With_Its_MaxEarnings()
    {
        // Legacy lines 3127-3132: no slab contains the rate => take the slab with the highest MaxEarnings
        // AND overwrite the rate with that MaxEarnings before applying the percentage.
        var wageType = Wage(1, "1");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 0,
            MaxEarnings = 2750,
            EmployeeContribution = 10,
            MinEarningsEmployeeContribution = 0
        };

        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 9999, wageTypes: new[] { wageType });

        await built.Employee.CalculateAsync(PifssContext(wageType));

        var line = built.Store.Lines.Single(l => l.WageTypeId == 1);
        line.Amount.ShouldBe(2750 * 10 / 100.0, 1e-9);
        line.Rate.ShouldBe(2750);            // legacy line 3190 writes the (replaced) rate
        line.Number.ShouldBe(0);             // legacy line 3189
    }

    [Fact]
    public async Task Pifss_Should_Leave_An_Existing_Line_Completely_Untouched()
    {
        // PRESERVED LEGACY QUIRK: legacy assigns Rate/Number/Amount only inside the "row did not exist" arm
        // (legacy lines 3183-3201).
        var wageType = Wage(1, "1");
        var slab = new SocialSecurityDetail
        {
            MinEarnings = 0, MaxEarnings = 999999, EmployeeContribution = 10
        };

        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });
        built.Store.Lines.Add(new PayrollRegisterLine { Id = 55, WageTypeId = 1, WageCode = "1", Rate = 1, Number = 2, Amount = 3 });

        await built.Employee.CalculateAsync(PifssContext(wageType));

        var line = built.Store.Lines.Single(l => l.WageTypeId == 1);
        line.Rate.ShouldBe(1);
        line.Number.ShouldBe(2);
        line.Amount.ShouldBe(3);
    }

    [Fact]
    public async Task Pifss_Should_Be_Skipped_On_An_AdvanceLeave_OffCycle_When_The_WageType_Is_Not_IncludeInAdvancePay()
    {
        var wageType = Wage(1, "1");
        wageType.IncludeInAdvancePay = false;

        var slab = new SocialSecurityDetail { MinEarnings = 0, MaxEarnings = 999999, EmployeeContribution = 10 };
        var built = BuildPifss(earningsCap: 0, slab: slab, registerRate: 1000, wageTypes: new[] { wageType });

        var context = PifssContext(wageType);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await built.Employee.CalculateAsync(context);

        built.Store.Lines.Any(l => l.WageTypeId == 1).ShouldBeFalse();
    }

    #endregion

    #region << 27 Average Commission -- period window (legacy 3853-3909) >>

    private static AverageCommissionWageTypeCalculator BuildAverageCommission(
        FakeRegisterStore store,
        FakeAdvancePaymentService advance,
        IEnumerable<EmployeePayment> payments,
        IEnumerable<EmployeeSeparation> separations = null)
    {
        var subGroupResolver = Substitute.For<IEmployeeSubGroupResolver>();
        subGroupResolver.GetEmployeeSubGroupIdAsync(Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult((int?)5));

        return new AverageCommissionWageTypeCalculator(
            store, advance, subGroupResolver,
            Repo(new EmployeeSubGroupCommission { Id = 1, TenantId = 1, EmployeeSubGroupId = 5, WageTypeId = 61 }),
            Repo(separations ?? Enumerable.Empty<EmployeeSeparation>()),
            Repo(payments));
    }

    [Fact]
    public async Task AverageCommission_Should_Divide_A_Rolling_Twelve_Month_Window_By_Twelve()
    {
        // Legacy lines 3871 + 3885: window is payDate.AddYears(-1).AddDays(1) .. payDate, divisor is 12.
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(27, "27", BillingType.Earning);

        var payments = new[]
        {
            // Inside the window (2024-04-01 .. 2025-03-31).
            new EmployeePayment { Id = 1, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 600, PaymentDate = new DateTime(2024, 4, 1) },
            new EmployeePayment { Id = 2, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 600, PaymentDate = new DateTime(2025, 3, 31) },
            // One day before the window opens -- must be excluded.
            new EmployeePayment { Id = 3, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 9999, PaymentDate = new DateTime(2024, 3, 31) },
            // A different wage type -- must be excluded.
            new EmployeePayment { Id = 4, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 62, Amount = 9999, PaymentDate = new DateTime(2024, 6, 1) }
        };

        await BuildAverageCommission(store, advance, payments).CalculateAsync(Context(wageType));

        var line = store.Lines.Single(l => l.WageTypeId == 27);
        line.Amount.ShouldBe(100);   // 1200 / 12
        line.Rate.ShouldBe(100);     // legacy writes the provision to BOTH Rate and Amount
    }

    [Fact]
    public async Task AverageCommission_Should_Clamp_The_Window_To_The_Separation_Month_End()
    {
        // Legacy lines 3859-3868: payDate is pulled back to monthEnd(LastDayInService) when it is later,
        // which slides the whole 12-month window back with it.
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(27, "27", BillingType.Earning);

        var separations = new[]
        {
            new EmployeeSeparation { Id = 1, TenantId = 1, EmployeeId = EmployeeId, LastDayInService = new DateTime(2024, 6, 15) }
        };

        var payments = new[]
        {
            // Inside the clamped window (2023-07-01 .. 2024-06-30).
            new EmployeePayment { Id = 1, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 1200, PaymentDate = new DateTime(2023, 7, 1) },
            // Inside the UNclamped window but after the clamped end -- must be excluded.
            new EmployeePayment { Id = 2, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 9999, PaymentDate = new DateTime(2024, 12, 1) }
        };

        await BuildAverageCommission(store, advance, payments, separations).CalculateAsync(Context(wageType));

        store.Lines.Single(l => l.WageTypeId == 27).Amount.ShouldBe(100);
    }

    [Fact]
    public async Task AverageCommission_Should_Post_The_Advance_Recovery_Negated_On_An_OffCycle_Run()
    {
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(27, "27", BillingType.Earning);
        wageType.IncludeInAdvancePay = true;

        var payments = new[]
        {
            new EmployeePayment { Id = 1, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 61, Amount = 1200, PaymentDate = new DateTime(2024, 6, 1) }
        };

        var context = Context(wageType, PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave);

        await BuildAverageCommission(store, advance, payments).CalculateAsync(context);

        advance.Postings.Single().Amount.ShouldBe(-100);
        advance.Postings.Single().PaymentDate.ShouldBe(new DateTime(2025, 3, 1));
    }

    #endregion

    #region << 31 Average Bonus -- period window (legacy 4017-4055) >>

    private static AverageBonusWageTypeCalculator BuildAverageBonus(
        FakeRegisterStore store,
        FakeAdvancePaymentService advance,
        Employee employee,
        IEnumerable<EmployeeNonPayrollTrans> transactions)
    {
        return new AverageBonusWageTypeCalculator(
            store, advance, Repo(employee), Repo(transactions),
            Repo(new WageType { Id = 53, TenantId = 1, Code = "1053", NameEn = "Bonus Pay" }));
    }

    [Fact]
    public async Task AverageBonus_Should_Average_Over_The_Whole_Service_Then_Divide_By_Twelve()
    {
        // Legacy lines 4024 + 4042-4046: window is JoiningDate..payDate, and the divisor is
        // transaction count THEN 12.
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(31, "31", BillingType.Earning);
        var employee = new Employee { Id = EmployeeId, TenantId = 1, Code = "E1", JoiningDate = new DateTime(2020, 1, 1) };

        var transactions = new[]
        {
            new EmployeeNonPayrollTrans { Id = 1, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 53, Amount = 1200, PaymentDate = new DateTime(2021, 5, 1) },
            new EmployeeNonPayrollTrans { Id = 2, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 53, Amount = 2400, PaymentDate = new DateTime(2023, 5, 1) },
            // Before joining -- outside the window.
            new EmployeeNonPayrollTrans { Id = 3, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 53, Amount = 9999, PaymentDate = new DateTime(2019, 5, 1) }
        };

        await BuildAverageBonus(store, advance, employee, transactions)
            .CalculateAsync(Context(wageType, employee: employee));

        // (1200 + 2400) / 2 / 12
        store.Lines.Single(l => l.WageTypeId == 31).Amount.ShouldBe(150, 1e-9);
    }

    [Fact]
    public async Task AverageBonus_Should_Be_Zero_When_The_Employee_Has_No_JoiningDate()
    {
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(31, "31", BillingType.Earning);
        var employee = new Employee { Id = EmployeeId, TenantId = 1, Code = "E1" };

        var transactions = new[]
        {
            new EmployeeNonPayrollTrans { Id = 1, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 53, Amount = 1200, PaymentDate = new DateTime(2021, 5, 1) }
        };

        await BuildAverageBonus(store, advance, employee, transactions)
            .CalculateAsync(Context(wageType, employee: employee));

        store.Lines.Any(l => l.WageTypeId == 31).ShouldBeFalse();
    }

    #endregion

    #region << 29 Average Ticket -- period window (legacy 3957-4013) >>

    [Fact]
    public async Task AverageTicket_Should_Price_Only_The_Latest_Ticket_PaymentDate_And_Divide_By_Twelve()
    {
        // Legacy lines 3982-4002: latest PaymentDate <= payDate, then every row with that exact date.
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(29, "29", BillingType.Earning);

        var subGroupResolver = Substitute.For<IEmployeeSubGroupResolver>();
        subGroupResolver.GetEmployeeSubGroupIdAsync(Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult((int?)5));

        var rateService = Substitute.For<IAirTicketRateService>();
        rateService.GetAirTicketRateAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>())
            .Returns(Task.FromResult(300.0));

        var calculator = new AverageTicketWageTypeCalculator(
            store, advance, subGroupResolver, rateService,
            Repo(new EmployeeSubGroupTicket
            {
                Id = 1, TenantId = 1, EmployeeSubGroupId = 5, TicketTypeId = 71,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(2099, 12, 31)
            }),
            Repo(new TicketType { Id = 71, TenantId = 1, Code = "1", NameEn = "Employee" }),
            Repo(
                new EmployeeTicket { Id = 1, TenantId = 1, EmployeeId = EmployeeId, TicketTypeId = 71, PaymentDate = new DateTime(2024, 8, 1), Compensate = 2, Amount = 500 },
                new EmployeeTicket { Id = 2, TenantId = 1, EmployeeId = EmployeeId, TicketTypeId = 71, PaymentDate = new DateTime(2024, 8, 1), Compensate = 2, Amount = 500 },
                // An older payment date -- must be ignored entirely.
                new EmployeeTicket { Id = 3, TenantId = 1, EmployeeId = EmployeeId, TicketTypeId = 71, PaymentDate = new DateTime(2023, 8, 1), Compensate = 9, Amount = 9999 }));

        await calculator.CalculateAsync(Context(wageType));

        // (300*2 + 300*2) / 12
        store.Lines.Single(l => l.WageTypeId == 29).Amount.ShouldBe(100, 1e-9);
    }

    [Fact]
    public async Task AverageTicket_Should_Be_Zero_When_The_Employee_Has_No_Ticket_History()
    {
        var store = new FakeRegisterStore();
        var advance = new FakeAdvancePaymentService();
        var wageType = Wage(29, "29", BillingType.Earning);

        var subGroupResolver = Substitute.For<IEmployeeSubGroupResolver>();
        subGroupResolver.GetEmployeeSubGroupIdAsync(Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult((int?)5));

        var calculator = new AverageTicketWageTypeCalculator(
            store, advance, subGroupResolver, Substitute.For<IAirTicketRateService>(),
            Repo(new EmployeeSubGroupTicket
            {
                Id = 1, TenantId = 1, EmployeeSubGroupId = 5, TicketTypeId = 71,
                StartDate = new DateTime(2000, 1, 1), EndDate = new DateTime(2099, 12, 31)
            }),
            Repo(new TicketType { Id = 71, TenantId = 1, Code = "1", NameEn = "Employee" }),
            Repo<EmployeeTicket>());

        await calculator.CalculateAsync(Context(wageType));

        store.Lines.Any(l => l.WageTypeId == 29).ShouldBeFalse();
    }

    #endregion

    #region << 9000 Net Pay (legacy 1057-1148) >>

    private static NetPayWageTypeCalculator BuildNetPay(
        FakeRegisterStore store,
        IPayrollRoundingService rounding,
        IEnumerable<WageType> wageTypes,
        IEnumerable<EmployeePayment> payments,
        Abp.Domain.Repositories.IRepository<EmployeePayment> paymentRepo = null)
    {
        var currencyService = Substitute.For<ICurrencyService>();
        currencyService.GetEmployeeCurrencyAsync(Arg.Any<int>())
            .Returns(Task.FromResult(new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Kuwaiti Dinar", NameAr = "د.ك" }));

        return new NetPayWageTypeCalculator(
            store, rounding, Repo(wageTypes),
            paymentRepo ?? Repo(payments),
            Repo(new Employee { Id = EmployeeId, TenantId = 1, Code = "E1" }),
            currencyService);
    }

    private static IPayrollRoundingService Rounding(int netPayDigits = 3)
    {
        var rounding = Substitute.For<IPayrollRoundingService>();
        rounding.ApplyRoundingAsync(Arg.Any<PayrollRegisterRef>(), Arg.Any<PayrollEngineSettings>())
            .Returns(Task.CompletedTask);
        rounding.GetNetPayRoundingDigitsAsync(Arg.Any<int>(), Arg.Any<PayrollEngineSettings>())
            .Returns(Task.FromResult(netPayDigits));
        return rounding;
    }

    [Fact]
    public async Task NetPay_Should_Sum_Earnings_Minus_Deductions_Ignoring_DontImpactNetPay_And_Provisions()
    {
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);

        var wageTypes = new[]
        {
            netPayWageType,
            new WageType { Id = 100, TenantId = 1, Code = "1000", NameEn = "Basic", BillingType = BillingType.Earning },
            new WageType { Id = 200, TenantId = 1, Code = "2000", NameEn = "Late", BillingType = BillingType.Deduction },
            new WageType { Id = 300, TenantId = 1, Code = "3000", NameEn = "Prov", BillingType = BillingType.Provision },
            new WageType { Id = 400, TenantId = 1, Code = "4000", NameEn = "NoImpact", BillingType = BillingType.Earning, DontImpactNetPay = true }
        };

        store.Lines.Add(new PayrollRegisterLine { Id = 1, WageTypeId = 100, WageCode = "1000", Amount = 1000 });
        store.Lines.Add(new PayrollRegisterLine { Id = 2, WageTypeId = 200, WageCode = "2000", Amount = 150 });
        store.Lines.Add(new PayrollRegisterLine { Id = 3, WageTypeId = 300, WageCode = "3000", Amount = 500 });
        store.Lines.Add(new PayrollRegisterLine { Id = 4, WageTypeId = 400, WageCode = "4000", Amount = 700 });

        await BuildNetPay(store, Rounding(), wageTypes, Enumerable.Empty<EmployeePayment>())
            .CalculateAsync(Context(netPayWageType));

        store.Lines.Single(l => l.WageTypeId == 9000).Amount.ShouldBe(850);
    }

    [Fact]
    public async Task NetPay_Should_Round_Before_Summing()
    {
        // Legacy line 1062: SetRoundoff is the FIRST statement of the 9000 branch.
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);
        var rounding = Rounding();

        await BuildNetPay(store, rounding, new[] { netPayWageType }, Enumerable.Empty<EmployeePayment>())
            .CalculateAsync(Context(netPayWageType));

        await rounding.Received(1).ApplyRoundingAsync(Arg.Any<PayrollRegisterRef>(), Arg.Any<PayrollEngineSettings>());
    }

    [Fact]
    public async Task NetPay_Should_Be_Written_Even_When_It_Is_Zero()
    {
        // Legacy line 1093 sits outside any zero test -- unlike every other calculated-pay branch.
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);

        await BuildNetPay(store, Rounding(), new[] { netPayWageType }, Enumerable.Empty<EmployeePayment>())
            .CalculateAsync(Context(netPayWageType));

        store.Lines.Single(l => l.WageTypeId == 9000).Amount.ShouldBe(0);
    }

    [Fact]
    public async Task NetPay_Should_Zero_A_Negative_Net_And_Carry_It_To_Next_Month_As_A_2011_Payment()
    {
        // Legacy lines 1080-1134.
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);

        var wageTypes = new[]
        {
            netPayWageType,
            new WageType { Id = 200, TenantId = 1, Code = "2000", NameEn = "Ded", BillingType = BillingType.Deduction },
            new WageType { Id = 211, TenantId = 1, Code = "2011", NameEn = "Last Month Recovery" }
        };

        store.Lines.Add(new PayrollRegisterLine { Id = 2, WageTypeId = 200, WageCode = "2000", Amount = 123.456 });

        var payments = new List<EmployeePayment>();
        var paymentRepo = Repo(payments);
        EmployeePayment inserted = null;
        paymentRepo.InsertOrUpdateAsync(Arg.Do<EmployeePayment>(p => inserted = p))
            .Returns(callInfo => Task.FromResult(callInfo.Arg<EmployeePayment>()));

        await BuildNetPay(store, Rounding(), wageTypes, payments, paymentRepo)
            .CalculateAsync(Context(netPayWageType));

        store.Lines.Single(l => l.WageTypeId == 9000).Amount.ShouldBe(0);

        inserted.ShouldNotBeNull();
        inserted.WageTypeId.ShouldBe(211);
        inserted.Amount.ShouldBe(123.456);
        inserted.PaymentDate.ShouldBe(new DateTime(2025, 4, 1));  // 1st of the FOLLOWING month
    }

    [Fact]
    public async Task NetPay_Should_Not_Carry_A_Negative_Net_Forward_On_An_OffCycle_Run()
    {
        // Legacy line 1082 restricts the carry-forward to PayrollRunType.Regular.
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);

        var wageTypes = new[]
        {
            netPayWageType,
            new WageType { Id = 200, TenantId = 1, Code = "2000", NameEn = "Ded", BillingType = BillingType.Deduction },
            new WageType { Id = 211, TenantId = 1, Code = "2011", NameEn = "Last Month Recovery" }
        };

        store.Lines.Add(new PayrollRegisterLine { Id = 2, WageTypeId = 200, WageCode = "2000", Amount = 50 });

        var payments = new List<EmployeePayment>();
        var paymentRepo = Repo(payments);
        EmployeePayment inserted = null;
        paymentRepo.InsertOrUpdateAsync(Arg.Do<EmployeePayment>(p => inserted = p))
            .Returns(callInfo => Task.FromResult(callInfo.Arg<EmployeePayment>()));

        await BuildNetPay(store, Rounding(), wageTypes, payments, paymentRepo)
            .CalculateAsync(Context(netPayWageType, PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService));

        store.Lines.Single(l => l.WageTypeId == 9000).Amount.ShouldBe(-50);
        inserted.ShouldBeNull();
    }

    [Fact]
    public async Task NetPay_Should_Zero_An_Existing_2011_Payment_When_There_Is_No_Recovery_This_Month()
    {
        // Legacy lines 1135-1145: zeroed, NOT deleted.
        var store = new FakeRegisterStore();
        var netPayWageType = Wage(9000, "9000", BillingType.Earning);

        var wageTypes = new[]
        {
            netPayWageType,
            new WageType { Id = 100, TenantId = 1, Code = "1000", NameEn = "Basic", BillingType = BillingType.Earning },
            new WageType { Id = 211, TenantId = 1, Code = "2011", NameEn = "Last Month Recovery" }
        };

        store.Lines.Add(new PayrollRegisterLine { Id = 1, WageTypeId = 100, WageCode = "1000", Amount = 1000 });

        var stale = new EmployeePayment
        {
            Id = 9, TenantId = 1, EmployeeId = EmployeeId, WageTypeId = 211,
            PaymentDate = new DateTime(2025, 4, 1), Amount = 77
        };

        await BuildNetPay(store, Rounding(), wageTypes, new[] { stale })
            .CalculateAsync(Context(netPayWageType));

        stale.Amount.ShouldBe(0);
    }

    #endregion

    #region << Shared helpers >>

    [Fact]
    public void GetRecoveryPeriods_Should_Return_One_First_Of_Month_Per_Spanned_Month()
    {
        // Legacy Program.GetRecoveryPeriods (Program.cs lines 2922-2939).
        var service = new PayrollAdvancePaymentService(
            Repo<EmployeePayment>(), Repo<WageType>(), Repo<Employee>(), Substitute.For<ICurrencyService>());

        var periods = service.GetRecoveryPeriods(new DateTime(2025, 1, 15), new DateTime(2025, 3, 31));

        periods.ShouldBe(new[]
        {
            new DateTime(2025, 1, 1),
            new DateTime(2025, 2, 1),
            new DateTime(2025, 3, 1)
        });
    }

    [Fact]
    public async Task AdvancePaymentDed_Should_No_Op_On_An_EndOfService_OffCycle_Run()
    {
        // Legacy Program.cs lines 2944-2947.
        var payments = new List<EmployeePayment>();
        var repo = Repo(payments);

        var service = new PayrollAdvancePaymentService(
            repo, Repo<WageType>(), Repo<Employee>(), Substitute.For<ICurrencyService>());

        await service.AdvancePaymentDedAsync(EmployeeId, new DateTime(2025, 3, 1), new DateTime(2025, 3, 20), 100,
            PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService, string.Empty, new PayrollEngineSettings());

        await repo.DidNotReceiveWithAnyArgs().InsertOrUpdateAsync(null);
    }

    /// <summary>
    /// A repository fake that actually PERSISTS: <c>GetAll()</c> projects the LIVE list and
    /// <c>InsertOrUpdateAsync</c> appends new rows to it. The shared <c>PayrollEngineFakes.Repo</c> copies the
    /// list and no-ops writes, which cannot express "did the upsert create a second row or merge into the
    /// first?" -- the exact question these tests exist to answer.
    /// </summary>
    private static Abp.Domain.Repositories.IRepository<EmployeePayment> LivePaymentRepo(List<EmployeePayment> payments)
    {
        var repository = Substitute.For<Abp.Domain.Repositories.IRepository<EmployeePayment>>();

        repository.GetAll().Returns(_ => payments.AsQueryable());
        repository.GetAllIncluding(Arg.Any<System.Linq.Expressions.Expression<Func<EmployeePayment, object>>[]>())
            .Returns(_ => payments.AsQueryable());

        repository.InsertOrUpdateAsync(Arg.Any<EmployeePayment>()).Returns(callInfo =>
        {
            var entity = callInfo.Arg<EmployeePayment>();
            if (!payments.Contains(entity))
            {
                entity.Id = payments.Count + 1;
                payments.Add(entity);
            }

            return Task.FromResult(entity);
        });

        return repository;
    }

    /// <summary>
    /// Builds a <see cref="PayrollAdvancePaymentService"/> over the given wage-code-2005 payment rows.
    /// </summary>
    private static PayrollAdvancePaymentService BuildAdvanceService(List<EmployeePayment> payments)
    {
        var currencyService = Substitute.For<ICurrencyService>();
        currencyService.GetEmployeeCurrencyAsync(Arg.Any<int>())
            .Returns(Task.FromResult(new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "KWD", NameAr = "KWD" }));

        return new PayrollAdvancePaymentService(
            LivePaymentRepo(payments),
            Repo(new WageType
            {
                Id = 2005, TenantId = 1, Code = PayrollEngineWageCodes.AdvancePaymentDeduction,
                NameEn = "Advance Payment Ded", Status = true
            }),
            Repo(new Employee { Id = EmployeeId, TenantId = 1, Code = "E1" }),
            currencyService);
    }

    /// <summary>
    /// WAVE P8 -- THE REASON <c>EmployeePayment.OffCyclePaymentDate</c> WAS RESTORED.
    ///
    /// <para>Legacy's upsert key is <c>(Employee, PaymentDate, OffCyclePaymentDate, WageType)</c> (legacy
    /// <c>Program.cs</c> lines 2948-2953). Two off-cycle runs with DIFFERENT payment dates that both recover
    /// against the same <c>paymentDate</c> month must produce TWO rows. Matching without
    /// <c>OffCyclePaymentDate</c> merged them into one and over-deducted -- real money.</para>
    /// </summary>
    [Fact]
    public async Task AdvancePaymentDed_Should_Keep_Separate_Rows_Per_OffCyclePaymentDate()
    {
        var payments = new List<EmployeePayment>();
        var service = BuildAdvanceService(payments);
        var paymentDate = new DateTime(2025, 3, 1);
        var settings = new PayrollEngineSettings { DecimalInSalaryRegister = 3 };

        await service.AdvancePaymentDedAsync(EmployeeId, paymentDate, new DateTime(2025, 3, 10), 100,
            PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave, "c", settings);
        await service.AdvancePaymentDedAsync(EmployeeId, paymentDate, new DateTime(2025, 3, 20), 100,
            PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave, "c", settings);

        payments.Count.ShouldBe(2);
        payments.Select(p => p.OffCyclePaymentDate).ShouldBe(
            new DateTime?[] { new DateTime(2025, 3, 10), new DateTime(2025, 3, 20) });
        payments.ShouldAllBe(p => p.Amount == 100);
    }

    /// <summary>
    /// The same off-cycle payment date DOES accumulate onto the existing row, rounded exactly as legacy
    /// (<c>Math.Round(existing + amount, decimalInSalaryRegister)</c>, legacy line 2982). 100.0005 + 0.0005
    /// at 3 digits is 100.001 (banker's rounding applies to the SUM, not the operands).
    /// </summary>
    [Fact]
    public async Task AdvancePaymentDed_Should_Accumulate_On_The_Same_Key_With_Legacy_Rounding()
    {
        var payments = new List<EmployeePayment>();
        var service = BuildAdvanceService(payments);
        var paymentDate = new DateTime(2025, 3, 1);
        var offCycleDate = new DateTime(2025, 3, 10);
        var settings = new PayrollEngineSettings { DecimalInSalaryRegister = 3 };

        await service.AdvancePaymentDedAsync(EmployeeId, paymentDate, offCycleDate, 100.0004,
            PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave, "c", settings);
        await service.AdvancePaymentDedAsync(EmployeeId, paymentDate, offCycleDate, 0.0003,
            PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave, "c", settings);

        payments.Count.ShouldBe(1);
        payments[0].Amount.ShouldBe(Math.Round(Math.Round(100.0004, 3) + 0.0003, 3));
        payments[0].IsSystemGenerated.ShouldBeTrue();
    }

    /// <summary>
    /// A REGULAR run has no off-cycle payment date; legacy's sentinel is <c>default(DateTime)</c>, stored
    /// as NULL here so the key stays consistent across both run types.
    /// </summary>
    [Fact]
    public async Task AdvancePaymentDed_Should_Store_Null_OffCyclePaymentDate_On_A_Regular_Run()
    {
        var payments = new List<EmployeePayment>();
        var service = BuildAdvanceService(payments);

        await service.AdvancePaymentDedAsync(EmployeeId, new DateTime(2025, 3, 1), default(DateTime), 50,
            PayrollRunType.Regular, null, "c", new PayrollEngineSettings { DecimalInSalaryRegister = 3 });

        payments.Count.ShouldBe(1);
        payments[0].OffCyclePaymentDate.ShouldBeNull();
        payments[0].IsSystemGenerated.ShouldBeTrue();
    }

    /// <summary>
    /// This test used to assert a documented <c>NotImplementedException</c> gap: <see cref="EmployeeOrgAssignment"/>
    /// carried no <c>EmployeeSubGroupId</c> FK, so the four wave-P6 rules that need the employee's sub-group had
    /// nothing to read. The FK now exists (migration
    /// <c>20260728103447_Added_EmployeeOrgAssignment_Group_SubGroup_PayrollArea_FKs</c>), so the assertion is
    /// inverted: the resolver must reproduce legacy's
    /// <c>empOrgAssignmentList.Where(r =&gt; r.StartDate &lt;= payDate &amp;&amp; r.EndDate &gt;= payDate)</c> lookup.
    /// </summary>
    [Fact]
    public async Task EmployeeSubGroupResolver_Should_Return_The_SubGroup_Of_The_Assignment_Covering_The_Date()
    {
        var resolver = new EmployeeSubGroupResolver(Repo(
            new EmployeeOrgAssignment
            {
                Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2025, 1, 31),
                EmployeeSubGroupId = 11
            },
            new EmployeeOrgAssignment
            {
                Id = 2, TenantId = 1, EmployeeId = EmployeeId,
                StartDate = new DateTime(2025, 2, 1), EndDate = new DateTime(2030, 12, 31),
                EmployeeSubGroupId = 22
            },
            new EmployeeOrgAssignment
            {
                Id = 3, TenantId = 1, EmployeeId = EmployeeId + 1,
                StartDate = new DateTime(2025, 2, 1), EndDate = new DateTime(2030, 12, 31),
                EmployeeSubGroupId = 33
            }));

        (await resolver.GetEmployeeSubGroupIdAsync(EmployeeId, new DateTime(2025, 3, 31))).ShouldBe(22);
        (await resolver.GetEmployeeSubGroupIdAsync(EmployeeId, new DateTime(2024, 6, 30))).ShouldBe(11);
    }

    /// <summary>
    /// The FK is nullable (rows written before the migration have no value) and no assignment may cover the
    /// date at all. Both are legitimately <c>null</c>; callers keep legacy's "EmployeeSubGroup Not found." throw.
    /// </summary>
    [Fact]
    public async Task EmployeeSubGroupResolver_Should_Return_Null_When_Nothing_Covers_The_Date_Or_The_Fk_Is_Unset()
    {
        var resolver = new EmployeeSubGroupResolver(Repo(
            new EmployeeOrgAssignment
            {
                Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31),
                EmployeeSubGroupId = 11
            },
            new EmployeeOrgAssignment
            {
                Id = 2, TenantId = 1, EmployeeId = EmployeeId,
                StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2030, 12, 31),
                EmployeeSubGroupId = null // pre-migration row
            }));

        (await resolver.GetEmployeeSubGroupIdAsync(EmployeeId, new DateTime(2025, 3, 31))).ShouldBeNull();
        (await resolver.GetEmployeeSubGroupIdAsync(EmployeeId, new DateTime(2000, 1, 1))).ShouldBeNull();
    }

    #endregion
}
