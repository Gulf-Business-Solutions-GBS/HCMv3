using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
/// The four clearing operations of <see cref="PayrollAdvancePaymentService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/Program.cs</c>: <c>DeleteAdvancePaymentDed</c> (lines 2988-3000) and the three
/// "Clear EmployeePayment Advance recovery" blocks (lines 1619-1653, 1796-1819 and 1832-1850).
///
/// <para><b>Why these matter more than they look.</b> <see cref="IPayrollAdvancePaymentService.AdvancePaymentDedAsync"/>
/// ACCUMULATES onto its row rather than replacing it, so a payroll that is re-run without clearing first
/// recovers twice what it advanced. Nothing errors -- the employee is simply short-paid a month later. These
/// were the last unported part of the off-cycle path.</para>
///
/// <para>Each test states the predicate it pins, because three of the four predicates are narrower than they
/// first appear and widening any of them would zero recoveries that legacy still collects.
/// <see cref="OffCycleAdvanceLeaveScenario_Tests"/> asserts the resulting money end to end; this file asserts
/// WHICH rows are touched.</para>
///
/// <para>Pure in-memory: the service uses only synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>
/// (Core has no EF reference), so a writable list fake is an exact stand-in.</para>
/// </summary>
public class PayrollAdvancePaymentService_Clearing_Tests
{
    private const int EmployeeId = 1;
    private const int OtherEmployeeId = 2;

    private const int AdvanceRecoveryWageTypeId = 2005;
    private const int LastMonthRecoveryWageTypeId = 2011;
    private const int UnrelatedWageTypeId = 1000;

    private static readonly DateTime OffCyclePaymentDate = new DateTime(2026, 10, 15);
    private static readonly DateTime October = new DateTime(2026, 10, 1);
    private static readonly DateTime November = new DateTime(2026, 11, 1);

    #region << Harness >>

    /// <summary>
    /// A WRITABLE in-memory repository: <c>DeleteAsync</c> removes from the list (the service's soft delete is
    /// ABP's concern, not this service's) and <c>UpdateAsync</c> is a no-op because the service mutates the very
    /// instances handed to it.
    /// </summary>
    private sealed class ListRepository<TEntity>
        where TEntity : class, Abp.Domain.Entities.IEntity<int>
    {
        private readonly List<TEntity> _items;
        private readonly IRepository<TEntity> _inner;

        public ListRepository(List<TEntity> items)
        {
            _items = items;
            _inner = Substitute.For<IRepository<TEntity>>();
            _inner.GetAll().Returns(_ => _items.AsQueryable());
            _inner.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => _items.AsQueryable());

            _inner.When(r => r.DeleteAsync(Arg.Any<TEntity>()))
                .Do(call => _items.Remove(call.Arg<TEntity>()));

            _inner.DeleteAsync(Arg.Any<TEntity>()).Returns(System.Threading.Tasks.Task.CompletedTask);
            _inner.UpdateAsync(Arg.Any<TEntity>()).Returns(call => System.Threading.Tasks.Task.FromResult(call.Arg<TEntity>()));
        }

        public IRepository<TEntity> Object => _inner;
    }

    private readonly List<EmployeePayment> _payments = new List<EmployeePayment>();

    private readonly List<WageType> _wageTypes = new List<WageType>
    {
        new WageType { Id = AdvanceRecoveryWageTypeId, TenantId = 1, Code = "2005", NameEn = "Advance Payment Deduction", BillingType = BillingType.Deduction },
        new WageType { Id = LastMonthRecoveryWageTypeId, TenantId = 1, Code = "2011", NameEn = "Last Month Recovery Deduction", BillingType = BillingType.Deduction },
        new WageType { Id = UnrelatedWageTypeId, TenantId = 1, Code = "1000", NameEn = "Basic Pay", BillingType = BillingType.Earning }
    };

    private PayrollAdvancePaymentService Build()
    {
        var employees = new List<Employee> { new Employee { Id = EmployeeId, TenantId = 1, Code = "E1" } };

        return new PayrollAdvancePaymentService(
            new ListRepository<EmployeePayment>(_payments).Object,
            new ListRepository<WageType>(_wageTypes).Object,
            new ListRepository<Employee>(employees).Object,
            Substitute.For<ICurrencyService>());
    }

    private EmployeePayment Payment(
        int wageTypeId,
        DateTime paymentDate,
        DateTime? offCyclePaymentDate,
        double amount,
        int employeeId = EmployeeId)
    {
        var payment = new EmployeePayment
        {
            Id = _payments.Count + 1,
            TenantId = 1,
            EmployeeId = employeeId,
            WageTypeId = wageTypeId,
            PaymentDate = paymentDate,
            OffCyclePaymentDate = offCyclePaymentDate,
            Amount = amount,
            UnitType = UnitType.Amount
        };

        _payments.Add(payment);
        return payment;
    }

    private EmployeePayment Advance(DateTime paymentDate, DateTime? offCyclePaymentDate, double amount, int employeeId = EmployeeId)
        => Payment(AdvanceRecoveryWageTypeId, paymentDate, offCyclePaymentDate, amount, employeeId);

    #endregion

    #region << DeleteAdvancePaymentDed (legacy lines 2988-3000) >>

    /// <summary>
    /// The predicate is <c>Employee + OffCyclePaymentDate + code 2005</c> -- note there is NO payment-date
    /// bound, so EVERY recovery month raised by that one off-cycle goes, which is exactly what a re-run needs.
    /// </summary>
    [Fact]
    public async Task DeleteAdvancePaymentDed_Should_Remove_Every_Recovery_Month_Of_That_OffCycle()
    {
        Advance(October, OffCyclePaymentDate, 260);
        Advance(November, OffCyclePaymentDate, 140);

        var deleted = await Build().DeleteAdvancePaymentDedAsync(EmployeeId, OffCyclePaymentDate);

        deleted.ShouldBe(2);
        _payments.ShouldBeEmpty();
    }

    /// <summary>
    /// <c>OffCyclePaymentDate</c> is part of the key for a reason: a recovery from a DIFFERENT off-cycle run is
    /// a separate debt and must survive. This is the case that made wave P8 restore the column.
    /// </summary>
    [Fact]
    public async Task DeleteAdvancePaymentDed_Should_Leave_Recoveries_From_Another_OffCycle_Run_Alone()
    {
        var thisRun = Advance(October, OffCyclePaymentDate, 260);
        var earlierRun = Advance(October, new DateTime(2026, 8, 12), 200);

        await Build().DeleteAdvancePaymentDedAsync(EmployeeId, OffCyclePaymentDate);

        _payments.ShouldNotContain(thisRun);
        _payments.ShouldContain(earlierRun);
        earlierRun.Amount.ShouldBe(200);
    }

    [Fact]
    public async Task DeleteAdvancePaymentDed_Should_Touch_Neither_Other_Employees_Nor_Other_Wage_Types()
    {
        var otherEmployee = Advance(October, OffCyclePaymentDate, 260, OtherEmployeeId);
        var otherWageType = Payment(UnrelatedWageTypeId, October, OffCyclePaymentDate, 500);
        var lastMonthRecovery = Payment(LastMonthRecoveryWageTypeId, October, OffCyclePaymentDate, 15);

        await Build().DeleteAdvancePaymentDedAsync(EmployeeId, OffCyclePaymentDate);

        _payments.ShouldContain(otherEmployee);
        _payments.ShouldContain(otherWageType);
        _payments.ShouldContain(lastMonthRecovery);
    }

    /// <summary>
    /// A regular run passes <c>default(DateTime)</c> (legacy's "no off-cycle" sentinel, stored as NULL). Nothing
    /// is keyed to it, so nothing may be deleted -- and in particular the rows a real off-cycle raised, whose
    /// column is NOT null, must not be swept up by a null match.
    /// </summary>
    [Fact]
    public async Task DeleteAdvancePaymentDed_Should_Do_Nothing_Without_An_OffCycle_Payment_Date()
    {
        Advance(October, OffCyclePaymentDate, 260);
        Advance(October, null, 99);

        var deleted = await Build().DeleteAdvancePaymentDedAsync(EmployeeId, default(DateTime));

        deleted.ShouldBe(0);
        _payments.Count.ShouldBe(2);
    }

    #endregion

    #region << ZeroFutureAdvanceRecovery (legacy lines 1619-1653) >>

    /// <summary>
    /// The advance-paid leave is gone, so the months still to be collected are abandoned -- but the CURRENT
    /// month's row stays, because that advance really was paid out. Hence the strict
    /// <c>PaymentDate &gt; payDate</c>.
    /// </summary>
    [Fact]
    public async Task ZeroFutureAdvanceRecovery_Should_Zero_Later_Months_And_Keep_The_Current_One()
    {
        // Legacy compares OffCyclePaymentDate against payDate, which on a regular run is the MONTH END.
        var monthEnd = new DateTime(2026, 10, 31);

        var currentMonth = Advance(monthEnd, monthEnd, 260);
        var nextMonth = Advance(November, monthEnd, 140);

        var zeroed = await Build().ZeroFutureAdvanceRecoveryAsync(EmployeeId, monthEnd, hasAdvancePaidLeaveOnPayDate: false);

        zeroed.ShouldBe(1);
        nextMonth.Amount.ShouldBe(0);
        nextMonth.IsSystemGenerated.ShouldBeTrue();
        currentMonth.Amount.ShouldBe(260);

        // Zeroed, NOT deleted -- the row stays visible as evidence the recovery was cancelled.
        _payments.Count.ShouldBe(2);
    }

    /// <summary>The leave is still on file, so its recovery must carry on untouched.</summary>
    [Fact]
    public async Task ZeroFutureAdvanceRecovery_Should_Do_Nothing_While_The_AdvancePaid_Leave_Exists()
    {
        var monthEnd = new DateTime(2026, 10, 31);
        var nextMonth = Advance(November, monthEnd, 140);

        var zeroed = await Build().ZeroFutureAdvanceRecoveryAsync(EmployeeId, monthEnd, hasAdvancePaidLeaveOnPayDate: true);

        zeroed.ShouldBe(0);
        nextMonth.Amount.ShouldBe(140);
    }

    /// <summary>
    /// <b>A legacy narrowness worth knowing when testing.</b> The match is <c>OffCyclePaymentDate == payDate</c>,
    /// and on a regular run <c>payDate</c> is the month END. An off-cycle paid on the 15th therefore does NOT
    /// match, and its recovery is never cleared by this route. Ported verbatim; this test exists so nobody
    /// "fixes" it without deciding to.
    /// </summary>
    [Fact]
    public async Task ZeroFutureAdvanceRecovery_Should_Not_Match_An_OffCycle_Paid_MidMonth()
    {
        var monthEnd = new DateTime(2026, 10, 31);
        var midMonthRun = Advance(November, OffCyclePaymentDate, 140);

        var zeroed = await Build().ZeroFutureAdvanceRecoveryAsync(EmployeeId, monthEnd, hasAdvancePaidLeaveOnPayDate: false);

        zeroed.ShouldBe(0);
        midMonthRun.Amount.ShouldBe(140);
    }

    #endregion

    #region << ZeroAdvanceRecoveryInWindow (legacy lines 1796-1819) >>

    /// <summary>
    /// The window applies to BOTH dates. Here the run covers 01/10 - 14/11 and the off-cycle was paid on 15/10,
    /// so both recovery rows fall inside it and both are zeroed before the run re-posts them.
    /// </summary>
    [Fact]
    public async Task ZeroAdvanceRecoveryInWindow_Should_Zero_Both_Recovery_Months_Of_This_Run()
    {
        var october = Advance(October, OffCyclePaymentDate, 260);
        var november = Advance(November, OffCyclePaymentDate, 140);

        var zeroed = await Build().ZeroAdvanceRecoveryInWindowAsync(EmployeeId, October, new DateTime(2026, 11, 14));

        zeroed.ShouldBe(2);
        october.Amount.ShouldBe(0);
        november.Amount.ShouldBe(0);
        october.IsSystemGenerated.ShouldBeTrue();
        _payments.Count.ShouldBe(2);
    }

    /// <summary>
    /// A row whose OFF-CYCLE date sits outside the window is left alone even when its payment date is inside --
    /// it belongs to a different off-cycle run. Legacy requires both bounds, and so does this.
    /// </summary>
    [Fact]
    public async Task ZeroAdvanceRecoveryInWindow_Should_Require_Both_Dates_To_Be_Inside()
    {
        var paymentInsideOffCycleOutside = Advance(October, new DateTime(2026, 8, 12), 200);
        var paymentOutsideOffCycleInside = Advance(new DateTime(2026, 12, 1), OffCyclePaymentDate, 90);

        var zeroed = await Build().ZeroAdvanceRecoveryInWindowAsync(EmployeeId, October, new DateTime(2026, 11, 14));

        zeroed.ShouldBe(0);
        paymentInsideOffCycleOutside.Amount.ShouldBe(200);
        paymentOutsideOffCycleInside.Amount.ShouldBe(90);
    }

    #endregion

    #region << ZeroLastMonthRecovery (legacy lines 1832-1850) >>

    /// <summary>
    /// Wage code 2011 carries a negative net forward into the NEXT month (see
    /// <see cref="NetPayWageTypeCalculator"/>). On a re-run the previous attempt's carry-forward must be cleared
    /// or the shortfall is collected twice, so the target is the 1st of the month AFTER the pay date.
    /// </summary>
    [Fact]
    public async Task ZeroLastMonthRecovery_Should_Zero_The_CarryForward_Dated_The_First_Of_Next_Month()
    {
        var carryForward = Payment(LastMonthRecoveryWageTypeId, November, null, 37.5);

        var zeroed = await Build().ZeroLastMonthRecoveryAsync(EmployeeId, new DateTime(2026, 10, 31));

        zeroed.ShouldBe(1);
        carryForward.Amount.ShouldBe(0);
        carryForward.IsSystemGenerated.ShouldBeTrue();
    }

    /// <summary>
    /// It keys on the payment date alone, NOT on <c>OffCyclePaymentDate</c> -- unlike the two 2005 blocks. A
    /// 2011 row is a carry-forward, not an advance, so it is not tied to an off-cycle run.
    /// </summary>
    [Fact]
    public async Task ZeroLastMonthRecovery_Should_Ignore_The_OffCycle_Payment_Date()
    {
        var carryForward = Payment(LastMonthRecoveryWageTypeId, November, OffCyclePaymentDate, 37.5);

        await Build().ZeroLastMonthRecoveryAsync(EmployeeId, new DateTime(2026, 10, 31));

        carryForward.Amount.ShouldBe(0);
    }

    [Fact]
    public async Task ZeroLastMonthRecovery_Should_Leave_Other_Months_And_The_2005_Rows_Alone()
    {
        var wrongMonth = Payment(LastMonthRecoveryWageTypeId, October, null, 12);
        var advance = Advance(November, OffCyclePaymentDate, 140);

        var zeroed = await Build().ZeroLastMonthRecoveryAsync(EmployeeId, new DateTime(2026, 10, 31));

        zeroed.ShouldBe(0);
        wrongMonth.Amount.ShouldBe(12);
        advance.Amount.ShouldBe(140);
    }

    #endregion

    #region << The property that ties them together >>

    /// <summary>
    /// The reason all of this exists, stated as one test: the upsert ACCUMULATES, so a second posting without a
    /// clear in between doubles the debt, and the clear is what makes the second run idempotent.
    /// </summary>
    [Fact]
    public async Task Clearing_Should_Make_A_Second_Posting_Replace_Rather_Than_Double_The_Recovery()
    {
        var service = Build();
        var settings = new PayrollEngineSettings { DecimalInSalaryRegister = 6 };
        var wageType2005 = _wageTypes.Single(w => w.Id == AdvanceRecoveryWageTypeId);

        // Seed the row the way an earlier off-cycle run left it, then post again WITHOUT clearing.
        var existing = Advance(October, OffCyclePaymentDate, 260);
        existing.WageTypeId = wageType2005.Id;

        await service.AdvancePaymentDedAsync(
            EmployeeId, October, OffCyclePaymentDate, 260, PayrollRunType.OffCycle,
            PayrollOffCycleReasons.AdvanceLeave, "advance", settings);

        existing.Amount.ShouldBe(520);   // the bug the clearing blocks prevent

        // Now clear first, as the engine does, and post again.
        await service.DeleteAdvancePaymentDedAsync(EmployeeId, OffCyclePaymentDate);
        _payments.ShouldBeEmpty();
    }

    #endregion
}
