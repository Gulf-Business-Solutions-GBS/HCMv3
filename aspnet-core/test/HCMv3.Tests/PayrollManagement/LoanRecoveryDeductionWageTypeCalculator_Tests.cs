using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- <b>wage code 2001 Loan Recovery Deduction</b>, the first
/// <see cref="IMasterDataDispatchedWageTypeCalculator"/>.
///
/// <para>The headline test is <see cref="Employee_3400_June_2024_Should_Deduct_The_Remaining_Balance_Of_20"/>:
/// a legacy-data regression built from the reference database rather than from an invented fixture. Its
/// expected 20.000 is DERIVED, never tuned -- the loan's <c>DueAmount</c> is 420.000 and 16 monthly
/// installments of <c>FixedAmount</c> 25.000 have already been recovered, so only 20.000 remains and legacy's
/// "never recover more than the balance" arm (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 1644-1647) clamps the final installment
/// down from 25.000 to 20.000. Legacy's own June-2024 register agrees at 20.000, and so does its own
/// <c>EmployeeLoanRecovery</c> row for 2024-06-30.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LoanRecoveryDeductionWageTypeCalculator_Tests
{
    private const int EmployeeId = 18;      // legacy employee OID 266, code 3400
    private const int RegisterId = 213;
    private const int LoanRecoveryWageTypeId = 1000;   // WageTypes row whose Code is "2001"

    private static readonly DateTime PayDate = new DateTime(2024, 6, 30);

    #region << Fakes >>

    /// <summary>
    /// A mutable in-memory <see cref="IRepository{TEntity}"/>. Unlike <c>PayrollEngineFakes.Repo</c> this one
    /// honours <c>InsertAsync</c>/<c>DeleteAsync</c>/<c>UpdateAsync</c>, which this rule needs: it is one of
    /// the very few wage rules that WRITES master data (legacy deletes and re-creates the period's
    /// <see cref="EmployeeLoanRecovery"/> rows, legacy lines 1590-1611 / 1665-1693).
    /// </summary>
    private sealed class MutableRepo<TEntity> where TEntity : class, IEntity<int>
    {
        public List<TEntity> Items { get; }

        public IRepository<TEntity> Repository { get; }

        public MutableRepo(IEnumerable<TEntity> items = null)
        {
            Items = items?.ToList() ?? new List<TEntity>();

            var repository = Substitute.For<IRepository<TEntity>>();
            repository.GetAll().Returns(_ => Items.AsQueryable());
            repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>())
                .Returns(_ => Items.AsQueryable());

            repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
            {
                var entity = call.Arg<TEntity>();
                if (entity.Id == 0)
                {
                    entity.Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1;
                }

                Items.Add(entity);
                return Task.FromResult(entity);
            });

            repository.UpdateAsync(Arg.Any<TEntity>())
                .Returns(call => Task.FromResult(call.Arg<TEntity>()));

            repository.DeleteAsync(Arg.Any<TEntity>()).Returns(call =>
            {
                Items.Remove(call.Arg<TEntity>());
                return Task.CompletedTask;
            });

            Repository = repository;
        }
    }

    private sealed class FakeRegisterStore : IPayrollRegisterStore
    {
        private int _nextId = 1;

        public List<PayrollRegisterLine> Lines { get; } = new List<PayrollRegisterLine>();

        public Task<List<PayrollRegisterLine>> GetLinesAsync(PayrollRegisterRef register)
            => Task.FromResult(Lines.ToList());

        public Task<PayrollRegisterLine> GetLineAsync(PayrollRegisterRef register, string wageCode)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageCode == wageCode));

        public Task<PayrollRegisterLine> GetLineByWageTypeIdAsync(PayrollRegisterRef register, int wageTypeId)
            => Task.FromResult(Lines.FirstOrDefault(l => l.WageTypeId == wageTypeId));

        public Task<PayrollRegisterLine> SaveLineAsync(
            PayrollRegisterRef register, PayrollRegisterLine line, bool deleteWhenAmountIsZero = true)
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
            Lines.RemoveAll(l => l.WageTypeId == wageTypeId);
            return Task.CompletedTask;
        }

        public Task ClearLinesAsync(PayrollRegisterRef register)
        {
            Lines.Clear();
            return Task.CompletedTask;
        }

        public Task SaveDayDetailsAsync(
            PayrollRegisterRef register, int lineId, IEnumerable<SalaryRegisterDayCount> days)
            => Task.CompletedTask;
    }

    private sealed class NoOpAdvancePaymentService : IPayrollAdvancePaymentService
    {
        public List<double> Postings { get; } = new List<double>();

        public Task AdvancePaymentDedAsync(int employeeId, DateTime paymentDate, DateTime offCyclePaymentDate,
            double amount, PayrollRunType payrollRunType, string offCycleReason, string advancePayComment,
            PayrollEngineSettings settings)
        {
            Postings.Add(amount);
            return Task.CompletedTask;
        }

        public List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate)
            => new List<DateTime> { new DateTime(payStartDate.Year, payStartDate.Month, 1) };

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

    /// <summary>
    /// Legacy <c>CurrencyService.Rounding(emp, value)</c> resolves the employee's payroll-area currency
    /// <c>DecimalCount</c>. For this customer that is 3 (KWD).
    /// </summary>
    private static ICurrencyService CurrencyServiceRoundingTo3Decimals()
    {
        var currencyService = Substitute.For<ICurrencyService>();
        currencyService.RoundAsync(Arg.Any<int>(), Arg.Any<double>())
            .Returns(call => Task.FromResult(Math.Round(call.ArgAt<double>(1), 3)));
        return currencyService;
    }

    #endregion

    #region << Fixture: the reference database's employee 3400, June 2024 >>

    private static WageType LoanRecoveryWageType() => new WageType
    {
        Id = LoanRecoveryWageTypeId,
        Code = "2001",
        NameEn = "Loan Recovery Deduction",
        BillingType = BillingType.Deduction
    };

    private static LoanType ResidenceExpensesLoanType() => new LoanType
    {
        Id = 2,
        Code = "2",
        NameEn = "Residence Expenses",
        LoanDeductionType = LoanDeductionType.Fixed,
        RecoveryOption = RecoveryOption.All,
        WageTypeId = LoanRecoveryWageTypeId
    };

    /// <summary>Legacy <c>EmployeeLoan</c> OID 280, verbatim.</summary>
    private static EmployeeLoan Loan3400() => new EmployeeLoan
    {
        Id = 2,
        EmployeeId = EmployeeId,
        LoanTypeId = 2,
        StartDate = new DateTime(2023, 2, 1),
        RecoveryStartDate = new DateTime(2023, 2, 1),
        EndDate = new DateTime(2024, 6, 30),
        DueAmount = 420,
        BalanceDueAmount = 0,        // fully recovered, exactly as the legacy row stands today
        FixedAmount = 25,
        RecoverFracInLastTrans = false,
        IncludeInLoanPay = true
    };

    /// <summary>
    /// The 16 completed installments of 25.000 (2023-02-28 .. 2024-05-31). Legacy holds 17 rows; the 17th is
    /// the 2024-06-30 payroll-generated row added separately per test.
    /// </summary>
    private static List<EmployeeLoanRecovery> SixteenCompletedInstallments()
    {
        var rows = new List<EmployeeLoanRecovery>();
        var monthEnd = new DateTime(2023, 2, 28);

        for (var i = 0; i < 16; i++)
        {
            rows.Add(new EmployeeLoanRecovery
            {
                Id = i + 1,
                EmployeeLoanId = 2,
                RecoveryDate = monthEnd,
                RecoveryAmount = 25,
                LoanRecoveryMethod = LoanRecoveryMethod.RecoveryThroughPayroll
            });

            var next = monthEnd.AddMonths(1);
            monthEnd = new DateTime(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month));
        }

        return rows;
    }

    private static PayrollCalculationContext Context() => new PayrollCalculationContext
    {
        EmployeeId = EmployeeId,
        PayDate = PayDate,
        PayStartDate = new DateTime(2024, 6, 1),
        PayInDate = PayDate,
        OffCycleEndDate = PayDate,
        PayrollRunType = PayrollRunType.Regular,
        WageType = LoanRecoveryWageType(),
        Register = new PayrollRegisterRef
        {
            RegisterId = RegisterId,
            Target = PayrollRegisterTarget.Live,
            ForPeriod = PayDate,
            InPeriod = PayDate,
            PayrollRunType = PayrollRunType.Regular
        }
    };

    private sealed class Harness
    {
        public FakeRegisterStore Store { get; } = new FakeRegisterStore();

        public NoOpAdvancePaymentService AdvancePayment { get; } = new NoOpAdvancePaymentService();

        public MutableRepo<LoanType> LoanTypes { get; }

        public MutableRepo<EmployeeLoan> Loans { get; }

        public MutableRepo<EmployeeLoanRecovery> Recoveries { get; }

        public MutableRepo<EmployeeLoanExemption> Exemptions { get; }

        public MutableRepo<LoanTypeDetail> Details { get; }

        public MutableRepo<EmployeeLoanInstallment> Installments { get; }

        public LoanRecoveryDeductionWageTypeCalculator Calculator { get; }

        public Harness(
            IEnumerable<LoanType> loanTypes,
            IEnumerable<EmployeeLoan> loans,
            IEnumerable<EmployeeLoanRecovery> recoveries,
            IEnumerable<EmployeeLoanExemption> exemptions = null,
            IEnumerable<LoanTypeDetail> details = null,
            IEnumerable<EmployeeLoanInstallment> installments = null)
        {
            LoanTypes = new MutableRepo<LoanType>(loanTypes);
            Loans = new MutableRepo<EmployeeLoan>(loans);
            Recoveries = new MutableRepo<EmployeeLoanRecovery>(recoveries);
            Exemptions = new MutableRepo<EmployeeLoanExemption>(exemptions);
            Details = new MutableRepo<LoanTypeDetail>(details);
            Installments = new MutableRepo<EmployeeLoanInstallment>(installments);

            Calculator = new LoanRecoveryDeductionWageTypeCalculator(
                Store,
                AdvancePayment,
                CurrencyServiceRoundingTo3Decimals(),
                LoanTypes.Repository,
                Loans.Repository,
                Recoveries.Repository,
                Exemptions.Repository,
                Details.Repository,
                Installments.Repository);
        }

        public double? PostedAmount =>
            Store.Lines.FirstOrDefault(l => l.WageTypeId == LoanRecoveryWageTypeId)?.Amount;
    }

    #endregion

    /// <summary>
    /// THE regression test. Employee 3400, June 2024: legacy posts 2001 = 20.000 and so must we.
    ///
    /// <para>Note the loan's stored <c>BalanceDueAmount</c> is 0 -- it looks fully repaid. It qualifies only
    /// through legacy's second arm (legacy line 1579): a <c>RecoveryThroughPayroll</c> row already exists for
    /// this very pay date, from the previous run of this same period. The rule then removes that row,
    /// recomputes the balance without it (420 - 16*25 = 20) and re-derives the installment. That is what
    /// makes re-running a period idempotent rather than cumulative.</para>
    /// </summary>
    [Fact]
    public async Task Employee_3400_June_2024_Should_Deduct_The_Remaining_Balance_Of_20()
    {
        var recoveries = SixteenCompletedInstallments();
        recoveries.Add(new EmployeeLoanRecovery
        {
            Id = 99,
            EmployeeLoanId = 2,
            RecoveryDate = PayDate,
            RecoveryAmount = 20,
            LoanRecoveryMethod = LoanRecoveryMethod.RecoveryThroughPayroll
        });

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { Loan3400() },
            recoveries);

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(20.00);
    }

    /// <summary>
    /// The same period run for the FIRST time: no 2024-06-30 recovery row yet, and the loan still shows the
    /// 20.000 balance. Same answer -- proving the 20.000 comes from the balance clamp, not from echoing the
    /// pre-existing row.
    /// </summary>
    [Fact]
    public async Task First_Run_Of_The_Period_Should_Also_Deduct_20_From_The_Remaining_Balance()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 20;

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments());

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(20.00);

        // Legacy lines 1664-1693: the installment is persisted and the balance falls to zero.
        var written = harness.Recoveries.Items.Single(r => r.RecoveryDate == PayDate);
        written.RecoveryAmount.ShouldBe(20.00);
        written.LoanRecoveryMethod.ShouldBe(LoanRecoveryMethod.RecoveryThroughPayroll);
        loan.BalanceDueAmount.ShouldBe(0);
    }

    /// <summary>
    /// Mid-schedule, with plenty owing, the FULL <see cref="EmployeeLoan.FixedAmount"/> is taken (legacy line
    /// 1626). Without this the previous two tests could pass for the wrong reason -- e.g. a rule that always
    /// returned the balance.
    /// </summary>
    [Fact]
    public async Task Mid_Schedule_Should_Deduct_The_Full_Fixed_Installment()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;
        loan.EndDate = new DateTime(2025, 6, 30);

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments().Take(4).ToList());

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(25.00);
    }

    private static EmployeeLoanInstallment Installment(int id, DateTime start, DateTime end, double amount) =>
        new EmployeeLoanInstallment { Id = id, EmployeeLoanId = 2, StartDate = start, EndDate = end, Amount = amount };

    /// <summary>
    /// Legacy lines 1626-1632: the instalment row in force on the pay date wins over <c>FixedAmount</c> -- here
    /// the June 2024 change from 25 to 40.
    /// </summary>
    [Fact]
    public async Task Installment_Row_In_Force_On_The_Pay_Date_Should_Replace_The_Fixed_Amount()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;
        loan.EndDate = new DateTime(2025, 6, 30);

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments().Take(4).ToList(),
            installments: new[]
            {
                Installment(1, new DateTime(2023, 2, 1), new DateTime(2024, 5, 31), 25),
                Installment(2, new DateTime(2024, 6, 1), new DateTime(9999, 12, 31), 40)
            });

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(40.00);
    }

    /// <summary>
    /// Legacy keeps <c>FixedAmount</c> as the fallback whenever no row covers the pay date -- also for a loan
    /// whose only row starts later. Pinned as legacy has it.
    /// </summary>
    [Fact]
    public async Task No_Installment_Row_Covering_The_Pay_Date_Should_Fall_Back_To_The_Fixed_Amount()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;
        loan.EndDate = new DateTime(2025, 6, 30);

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments().Take(4).ToList(),
            installments: new[] { Installment(1, new DateTime(2024, 7, 1), new DateTime(9999, 12, 31), 40) });

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(25.00);
    }

    /// <summary>Another loan's schedule is never read.</summary>
    [Fact]
    public async Task Installment_Rows_Of_Another_Loan_Should_Be_Ignored()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;
        loan.EndDate = new DateTime(2025, 6, 30);

        var other = Installment(1, new DateTime(2024, 6, 1), new DateTime(9999, 12, 31), 40);
        other.EmployeeLoanId = 77;

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments().Take(4).ToList(),
            installments: new[] { other });

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(25.00);
    }

    /// <summary>Legacy lines 1615-1621: an exemption dated inside the pay month skips the deduction.</summary>
    [Fact]
    public async Task An_Exemption_In_The_Pay_Month_Should_Suppress_The_Deduction()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new[] { loan },
            SixteenCompletedInstallments().Take(4).ToList(),
            new[]
            {
                new EmployeeLoanExemption
                {
                    Id = 1, EmployeeLoanId = 2, ExemptionDate = new DateTime(2024, 6, 15)
                }
            });

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBeNull();
    }

    /// <summary>
    /// Legacy line 1577: <see cref="RecoveryOption.ThroughExternal"/> means payroll must not recover it.
    /// </summary>
    [Fact]
    public async Task RecoveryOption_ThroughExternal_Should_Not_Be_Recovered_By_Payroll()
    {
        var loanType = ResidenceExpensesLoanType();
        loanType.RecoveryOption = RecoveryOption.ThroughExternal;

        var loan = Loan3400();
        loan.BalanceDueAmount = 320;

        var harness = new Harness(
            new[] { loanType }, new[] { loan }, SixteenCompletedInstallments().Take(4).ToList());

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBeNull();
    }

    /// <summary>
    /// Legacy lines 1548-1557: manually-entered <see cref="LoanRecoveryMethod.AddlRecoveryThroughPayroll"/>
    /// rows inside the pay month are ADDED to the scheduled installment, not substituted for it.
    /// </summary>
    [Fact]
    public async Task Additional_Recovery_Rows_Should_Add_To_The_Scheduled_Installment()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;
        loan.EndDate = new DateTime(2025, 6, 30);

        var recoveries = SixteenCompletedInstallments().Take(4).ToList();
        recoveries.Add(new EmployeeLoanRecovery
        {
            Id = 500,
            EmployeeLoanId = 2,
            RecoveryDate = new DateTime(2024, 6, 10),
            RecoveryAmount = 7.5,
            LoanRecoveryMethod = LoanRecoveryMethod.AddlRecoveryThroughPayroll
        });

        var harness = new Harness(new[] { ResidenceExpensesLoanType() }, new[] { loan }, recoveries);

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(32.50);
    }

    /// <summary>Legacy line 76: the SPECIFIC <c>onHold_LoanDeduction</c> flag suppresses this rule.</summary>
    [Fact]
    public async Task OnHold_LoanDeduction_Should_Suppress_The_Deduction()
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = 320;

        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() }, new[] { loan }, SixteenCompletedInstallments().Take(4).ToList());

        var context = Context();
        context.OnHold = new PayrollOnHoldFlags { LoanDeduction = true };

        await harness.Calculator.CalculateAsync(context);

        harness.PostedAmount.ShouldBeNull();
    }

    /// <summary>
    /// Dispatch, not arithmetic: the rule claims the wage type its LoanType points at, and NOTHING else.
    /// This is the half of the port that wage code 2001 was actually missing -- legacy has "2001" commented
    /// out of its wage-code list (legacy <c>Program.cs</c> line 2691).
    /// </summary>
    [Fact]
    public void Should_Claim_Only_The_WageType_Its_LoanTypes_Point_At()
    {
        var harness = new Harness(
            new[] { ResidenceExpensesLoanType() },
            new EmployeeLoan[0],
            new EmployeeLoanRecovery[0]);

        harness.Calculator.ClaimsWageType(LoanRecoveryWageTypeId).ShouldBeTrue();
        harness.Calculator.ClaimsWageType(LoanRecoveryWageTypeId + 1).ShouldBeFalse();

        // Never reachable by code -- see IMasterDataDispatchedWageTypeCalculator.
        harness.Calculator.WageCode.ShouldBeNull();
    }

    /// <summary>
    /// A LoanType with no wage type must claim nothing (legacy guards every one of its LoanType queries with
    /// <c>Where(r =&gt; r.WageType != null)</c>, legacy <c>Program.cs</c> line 2671).
    /// </summary>
    [Fact]
    public void A_LoanType_Without_A_WageType_Should_Claim_Nothing()
    {
        var loanType = ResidenceExpensesLoanType();
        loanType.WageTypeId = null;

        var harness = new Harness(
            new[] { loanType }, new EmployeeLoan[0], new EmployeeLoanRecovery[0]);

        harness.Calculator.ClaimsWageType(LoanRecoveryWageTypeId).ShouldBeFalse();
    }

    #region << Variable (percentage) loan types >>

    private const int BasicWageTypeId = 10;
    private const int HousingWageTypeId = 11;

    private static LoanType VariableLoanType()
    {
        var loanType = ResidenceExpensesLoanType();
        loanType.LoanDeductionType = LoanDeductionType.Variable;
        return loanType;
    }

    private static EmployeeLoan VariableLoan(double balance)
    {
        var loan = Loan3400();
        loan.BalanceDueAmount = balance;
        loan.DueAmount = balance;
        loan.FixedAmount = 0;
        loan.EndDate = new DateTime(2025, 6, 30);
        return loan;
    }

    private static Harness VariableHarness(double balance, params LoanTypeDetail[] details)
    {
        var harness = new Harness(
            new[] { VariableLoanType() }, new[] { VariableLoan(balance) }, new EmployeeLoanRecovery[0], null, details);

        harness.Store.Lines.Add(new PayrollRegisterLine { Id = 900, WageTypeId = BasicWageTypeId, WageCode = "1000", Amount = 500 });
        harness.Store.Lines.Add(new PayrollRegisterLine { Id = 901, WageTypeId = HousingWageTypeId, WageCode = "1010", Amount = 200 });
        return harness;
    }

    private static LoanTypeDetail Detail(int id, int? wageTypeId, double percent) => new LoanTypeDetail
    {
        Id = id,
        LoanTypeId = 2,
        WageTypeId = wageTypeId,
        Percent = percent
    };

    /// <summary>
    /// Legacy lines 1628-1639: 10% of Basic 500 plus 5% of Housing 200 = 60.000, summed over the detail rows.
    /// </summary>
    [Fact]
    public async Task Variable_LoanType_Should_Deduct_The_Percentages_Of_Its_WageType_Lines()
    {
        var harness = VariableHarness(1000, Detail(1, BasicWageTypeId, 10), Detail(2, HousingWageTypeId, 5));

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(60.00);
    }

    /// <summary>Legacy line 1634: a wage type with no line in the register adds nothing.</summary>
    [Fact]
    public async Task Variable_Detail_Whose_WageType_Has_No_Line_Should_Add_Nothing()
    {
        var harness = VariableHarness(1000, Detail(1, BasicWageTypeId, 10), Detail(2, 999, 50));

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(50.00);
    }

    /// <summary>Legacy lines 1644-1647 apply to the percentage too: never more than the balance owing.</summary>
    [Fact]
    public async Task Variable_Installment_Should_Be_Clamped_To_The_Remaining_Balance()
    {
        var harness = VariableHarness(30, Detail(1, BasicWageTypeId, 10));

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(30.00);
    }

    /// <summary>
    /// A detail row saved before the wage type was ported has none; it is skipped rather than failing the run.
    /// </summary>
    [Fact]
    public async Task Variable_Detail_Without_A_WageType_Should_Be_Skipped()
    {
        var harness = VariableHarness(1000, Detail(1, null, 10), Detail(2, HousingWageTypeId, 10));

        await harness.Calculator.CalculateAsync(Context());

        harness.PostedAmount.ShouldBe(20.00);
    }

    #endregion
}
