using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- <see cref="WageTypeCalculatorResolver"/>: the single dispatcher that replaces the legacy
/// engine's <c>if (currWageType.Code == "xxxx") ... else if ...</c> chains, and the ONE place the four legacy
/// pre-calculation gates now live.
///
/// <para>Gates under test (see <see cref="IWageTypeCalculatorResolver"/> for the legacy line references):
/// <c>Program.Allow_CalcWageType</c>, the <c>IsNotReturnFromLeave</c> gate,
/// <c>EmployeeCalculatedPayService.IsOnHold</c> and <c>EmployeeCalculatedPayService.AllowToCalculate</c>.
/// Getting any of these wrong pays money that should not be paid (or withholds money that should be), so
/// each is asserted on both arms.</para>
///
/// <para>The resolver is constructed directly with in-memory repositories -- it uses only synchronous
/// <c>System.Linq</c> over <c>IRepository&lt;T&gt;.GetAll()</c> by design (HCMv3.Core has no EF Core
/// reference), so no database is needed. DI registration is covered separately by
/// <see cref="WageTypeCalculatorRegistration_Tests"/>.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WageTypeCalculatorResolver_Tests
{
    private const int EmployeeId = 7;
    private static readonly DateTime PayDate = new DateTime(2025, 3, 31);

    #region << Test doubles >>

    /// <summary>
    /// A recording <see cref="IWageTypeCalculator"/>. Named with the <c>...WageTypeCalculator</c> suffix on
    /// purpose -- see <see cref="WageTypeCalculatorRegistration_Tests"/> for why the suffix matters.
    /// </summary>
    private sealed class RecordingWageTypeCalculator : IWageTypeCalculator
    {
        public RecordingWageTypeCalculator(string wageCode)
        {
            WageCode = wageCode;
        }

        public string WageCode { get; }

        public int CallCount { get; private set; }

        public Task CalculateAsync(PayrollCalculationContext context)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private static WageType Wage(int id, string code, BillingType billingType = BillingType.Earning)
    {
        return new WageType
        {
            Id = id,
            TenantId = 1,
            Code = code,
            NameEn = "Wage " + code,
            NameAr = "Wage " + code,
            BillingType = billingType,
            PayrollOrder = id,
            Status = true
        };
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        PayrollOnHoldFlags onHold = null,
        bool isNotReturnFromLeave = false,
        int registerId = 100)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            PayDate = PayDate,
            PayStartDate = new DateTime(2025, 3, 1),
            PayInDate = PayDate,
            WageType = wageType,
            OnHold = onHold ?? new PayrollOnHoldFlags(),
            IsNotReturnFromLeave = isNotReturnFromLeave,
            Register = new PayrollRegisterRef
            {
                RegisterId = registerId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayDate,
                InPeriod = PayDate,
                PayrollRunType = PayrollRunType.Regular
            }
        };
    }

    private static WageTypeCalculatorResolver BuildResolver(
        IEnumerable<IWageTypeCalculator> calculators = null,
        IEnumerable<WageTypeException> wageTypeExceptions = null,
        IEnumerable<SalaryRegisterDetail> registerDetails = null,
        IEnumerable<EmployeeSeparation> separations = null,
        IEnumerable<EmployeeRejoin> rejoins = null)
    {
        return new WageTypeCalculatorResolver(
            calculators ?? Enumerable.Empty<IWageTypeCalculator>(),
            Repo(wageTypeExceptions ?? Enumerable.Empty<WageTypeException>()),
            Repo(registerDetails ?? Enumerable.Empty<SalaryRegisterDetail>()),
            Repo(separations ?? Enumerable.Empty<EmployeeSeparation>()),
            Repo(rejoins ?? Enumerable.Empty<EmployeeRejoin>()));
    }

    #endregion

    #region << Resolve >>

    [Fact]
    public void Resolve_Should_Match_On_WageCode()
    {
        var loan = new RecordingWageTypeCalculator("1006");
        var netPay = new RecordingWageTypeCalculator("9000");
        var resolver = BuildResolver(new IWageTypeCalculator[] { loan, netPay });

        resolver.Resolve("1006").ShouldBeSameAs(loan);
        resolver.Resolve("9000").ShouldBeSameAs(netPay);
    }

    [Fact]
    public void Resolve_Should_Return_Null_For_An_Unclaimed_Wage_Code()
    {
        // Normal and expected: legacy iterated EVERY active non-TimeType wage type and fell straight through
        // its else-if chain for codes it had no branch for.
        var resolver = BuildResolver(new IWageTypeCalculator[] { new RecordingWageTypeCalculator("1006") });

        resolver.Resolve("4711").ShouldBeNull();
    }

    [Fact]
    public void Resolve_Should_Be_Case_Sensitive_And_Null_Safe()
    {
        var resolver = BuildResolver(new IWageTypeCalculator[] { new RecordingWageTypeCalculator("1006") });

        resolver.Resolve(null).ShouldBeNull();
        resolver.Resolve("").ShouldBeNull();
    }

    [Fact]
    public void Resolve_Should_Throw_When_Two_Calculators_Claim_The_Same_Wage_Code()
    {
        // Legacy could not even express this ambiguity (its branches were literal else-ifs), so there is no
        // defined precedence to silently fall back on.
        var resolver = BuildResolver(new IWageTypeCalculator[]
        {
            new RecordingWageTypeCalculator("1006"),
            new RecordingWageTypeCalculator("1006")
        });

        var ex = Should.Throw<InvalidOperationException>(() => resolver.Resolve("1006"));
        ex.Message.ShouldContain("1006");
    }

    [Fact]
    public void Resolve_Should_Tolerate_A_Null_Calculator_Enumerable()
    {
        var resolver = new WageTypeCalculatorResolver(
            null,
            Repo<WageTypeException>(),
            Repo<SalaryRegisterDetail>(),
            Repo<EmployeeSeparation>(),
            Repo<EmployeeRejoin>());

        resolver.Resolve("1006").ShouldBeNull();
    }

    #endregion

    #region << TryCalculateAsync: happy path and unclaimed codes >>

    [Fact]
    public async Task TryCalculateAsync_Should_Invoke_The_Matching_Calculator()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
        calculator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task TryCalculateAsync_Should_Return_False_For_An_Unclaimed_Wage_Code()
    {
        var resolver = BuildResolver();

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
    }

    [Fact]
    public async Task TryCalculateAsync_Should_Throw_When_WageType_Is_Not_Set()
    {
        var resolver = BuildResolver();

        await Should.ThrowAsync<ArgumentException>(() => resolver.TryCalculateAsync(new PayrollCalculationContext()));
        await Should.ThrowAsync<ArgumentException>(() => resolver.TryCalculateAsync(null));
    }

    #endregion

    #region << Gate 1: Program.Allow_CalcWageType (legacy Program.cs 2752-2770) >>

    [Fact]
    public async Task Gate_AllowCalcWageType_Should_Skip_When_An_Excepted_WageType_Has_A_NonZero_Line()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            wageTypeExceptions: new[] { new WageTypeException { TenantId = 1, WageTypeId = 1, ExceptWageTypeId = 2 } },
            registerDetails: new[]
            {
                new SalaryRegisterDetail { Id = 1, TenantId = 1, SalaryRegisterId = 100, WageTypeId = 2, Amount = 50 }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
        calculator.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    public async Task Gate_AllowCalcWageType_Should_Trip_On_Any_Of_Rate_Number_Or_Amount(double rate, double number, double amount)
    {
        // Legacy tests all three fields (line 2762), not just Amount.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            wageTypeExceptions: new[] { new WageTypeException { TenantId = 1, WageTypeId = 1, ExceptWageTypeId = 2 } },
            registerDetails: new[]
            {
                new SalaryRegisterDetail
                {
                    Id = 1, TenantId = 1, SalaryRegisterId = 100, WageTypeId = 2,
                    Rate = rate, Number = number, Amount = amount
                }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
    }

    [Fact]
    public async Task Gate_AllowCalcWageType_Should_Not_Skip_When_The_Excepted_Line_Is_All_Zero()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            wageTypeExceptions: new[] { new WageTypeException { TenantId = 1, WageTypeId = 1, ExceptWageTypeId = 2 } },
            registerDetails: new[]
            {
                new SalaryRegisterDetail { Id = 1, TenantId = 1, SalaryRegisterId = 100, WageTypeId = 2 }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_AllowCalcWageType_Should_Only_Look_At_The_Current_Register()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            wageTypeExceptions: new[] { new WageTypeException { TenantId = 1, WageTypeId = 1, ExceptWageTypeId = 2 } },
            registerDetails: new[]
            {
                // A non-zero excepted line, but on somebody else's register.
                new SalaryRegisterDetail { Id = 1, TenantId = 1, SalaryRegisterId = 999, WageTypeId = 2, Amount = 50 }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    #endregion

    #region << Gate 2: IsNotReturnFromLeave (legacy EmployeeCalculatedPayService.cs line 35) >>

    [Fact]
    public async Task Gate_IsNotReturnFromLeave_Should_Skip_An_Ordinary_Earning()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006"), isNotReturnFromLeave: true))).ShouldBeFalse();
    }

    [Fact]
    public async Task Gate_IsNotReturnFromLeave_Should_Still_Allow_Provision_WageTypes()
    {
        var calculator = new RecordingWageTypeCalculator("3000");
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        var context = Context(Wage(1, "3000", BillingType.Provision), isNotReturnFromLeave: true);

        (await resolver.TryCalculateAsync(context)).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_IsNotReturnFromLeave_Should_Still_Allow_9000_Net_Pay()
    {
        var calculator = new RecordingWageTypeCalculator(PayrollEngineWageCodes.NetPay);
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        var context = Context(Wage(1, PayrollEngineWageCodes.NetPay), isNotReturnFromLeave: true);

        (await resolver.TryCalculateAsync(context)).ShouldBeTrue();
    }

    #endregion

    #region << Gate 3: IsOnHold (legacy EmployeeCalculatedPayService.cs line 1160) >>

    [Fact]
    public void IsOnHold_Should_Hold_Earnings_Only_On_AllEarnings()
    {
        var flags = new PayrollOnHoldFlags { AllEarnings = true };

        WageTypeCalculatorResolver.IsOnHold(Wage(1, "1006", BillingType.Earning), flags).ShouldBeTrue();
        WageTypeCalculatorResolver.IsOnHold(Wage(2, "2000", BillingType.Deduction), flags).ShouldBeFalse();
    }

    [Fact]
    public void IsOnHold_Should_Hold_Deductions_Only_On_AllDeductions()
    {
        var flags = new PayrollOnHoldFlags { AllDeductions = true };

        WageTypeCalculatorResolver.IsOnHold(Wage(2, "2000", BillingType.Deduction), flags).ShouldBeTrue();
        WageTypeCalculatorResolver.IsOnHold(Wage(1, "1006", BillingType.Earning), flags).ShouldBeFalse();
    }

    [Fact]
    public void IsOnHold_Should_Never_Gate_Provision_Or_TimeType_WageTypes()
    {
        // Legacy line 1160-1163 says nothing about Provision/TimeType -- deliberately preserved.
        var flags = new PayrollOnHoldFlags { AllEarnings = true, AllDeductions = true };

        WageTypeCalculatorResolver.IsOnHold(Wage(3, "3000", BillingType.Provision), flags).ShouldBeFalse();
        WageTypeCalculatorResolver.IsOnHold(Wage(4, "0100", BillingType.TimeType), flags).ShouldBeFalse();
    }

    [Fact]
    public void IsOnHold_Should_Ignore_The_Five_Specific_Holds()
    {
        // BasicPay/LeavePay/IndemnityPay/AirTicketPay/LoanDeduction remain each individual rule's business
        // (see PayrollCalculationContext.OnHold remarks) -- the generic gate must NOT sweep them up.
        var flags = new PayrollOnHoldFlags
        {
            BasicPay = true,
            LeavePay = true,
            IndemnityPay = true,
            AirTicketPay = true,
            LoanDeduction = true
        };

        WageTypeCalculatorResolver.IsOnHold(Wage(1, "1006", BillingType.Earning), flags).ShouldBeFalse();
        WageTypeCalculatorResolver.IsOnHold(Wage(2, "2000", BillingType.Deduction), flags).ShouldBeFalse();
    }

    [Fact]
    public void IsOnHold_Should_Be_Null_Safe()
    {
        WageTypeCalculatorResolver.IsOnHold(null, new PayrollOnHoldFlags { AllEarnings = true }).ShouldBeFalse();
        WageTypeCalculatorResolver.IsOnHold(Wage(1, "1006"), null).ShouldBeFalse();
    }

    [Fact]
    public async Task Gate_IsOnHold_Should_Skip_Dispatch_For_A_Held_Earning()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        var context = Context(Wage(1, "1006"), onHold: new PayrollOnHoldFlags { AllEarnings = true });

        (await resolver.TryCalculateAsync(context)).ShouldBeFalse();
        calculator.CallCount.ShouldBe(0);
    }

    [Fact]
    public void AnyHold_Should_Be_True_For_Each_Individual_Flag()
    {
        // Legacy line 1387 uses this to decide whether to reset EmployeePayrollStatus after the run.
        new PayrollOnHoldFlags().AnyHold.ShouldBeFalse();
        new PayrollOnHoldFlags { All = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { BasicPay = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { AirTicketPay = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { IndemnityPay = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { LeavePay = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { LoanDeduction = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { AllEarnings = true }.AnyHold.ShouldBeTrue();
        new PayrollOnHoldFlags { AllDeductions = true }.AnyHold.ShouldBeTrue();
    }

    #endregion

    #region << Gate 4: AllowToCalculate -- separation / rejoin (legacy lines 1164-1197) >>

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Allow_When_There_Is_No_Separation()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(new IWageTypeCalculator[] { calculator });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Skip_When_The_Separation_Month_Ended_Before_The_Pay_Date()
    {
        // Separated in February; calculating March -> nothing to pay.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 2, 10)
                }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
        calculator.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Allow_Within_The_Separation_Month()
    {
        // Legacy compares the MONTH END of LastDayInService, so a mid-March leaver is still paid for March.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 3, 10)
                }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Ignore_A_Revoked_Separation()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 2, 10),
                    RevokeDate = new DateTime(2025, 2, 20)
                }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Ignore_Another_Employees_Separation()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId + 1,
                    LastDayInService = new DateTime(2025, 2, 10)
                }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Allow_A_Rejoiner_Whose_Old_Separation_Predates_The_Rejoin()
    {
        // PROPERTY RENAME under test: legacy reads EmployeeRejoin.StartDate; this port's entity calls the
        // same field RejoinDate. With a rejoin present, only separations on/after the rejoin date count --
        // the pre-rejoin separation must be ignored, otherwise a rejoined employee is never paid again.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2024, 6, 30)
                }
            },
            rejoins: new[]
            {
                new EmployeeRejoin { Id = 1, TenantId = 1, EmployeeId = EmployeeId, RejoinDate = new DateTime(2025, 1, 1) }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeTrue();
        calculator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Skip_A_Rejoiner_Who_Separated_Again_In_An_Earlier_Month()
    {
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 2, 10)
                }
            },
            rejoins: new[]
            {
                new EmployeeRejoin { Id = 1, TenantId = 1, EmployeeId = EmployeeId, RejoinDate = new DateTime(2025, 1, 1) }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Ignore_A_Rejoin_Dated_After_The_Pay_Date()
    {
        // A future rejoin must not rescue an employee separated before the period being calculated.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 1, 15)
                }
            },
            rejoins: new[]
            {
                new EmployeeRejoin { Id = 1, TenantId = 1, EmployeeId = EmployeeId, RejoinDate = new DateTime(2025, 6, 1) }
            });

        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
    }

    [Fact]
    public async Task Gate_AllowToCalculate_Should_Ignore_Sentinel_Rejoin_Dates()
    {
        // Legacy line 1172 guards against the SqlDateTime minimum (1753-01-01); this port also treats
        // default(DateTime) as "unset". Neither may be treated as a real rejoin.
        var calculator = new RecordingWageTypeCalculator("1006");
        var resolver = BuildResolver(
            new IWageTypeCalculator[] { calculator },
            separations: new[]
            {
                new EmployeeSeparation
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    LastDayInService = new DateTime(2025, 2, 10)
                }
            },
            rejoins: new[]
            {
                new EmployeeRejoin { Id = 1, TenantId = 1, EmployeeId = EmployeeId, RejoinDate = new DateTime(1753, 1, 1) },
                new EmployeeRejoin { Id = 2, TenantId = 1, EmployeeId = EmployeeId, RejoinDate = default(DateTime) }
            });

        // The sentinels are discarded, so the "no rejoin" arm runs and the February separation blocks March.
        (await resolver.TryCalculateAsync(Context(Wage(1, "1006")))).ShouldBeFalse();
    }

    #endregion

    #region << Wave P9: master-table dispatch (IMasterDataDispatchedWageTypeCalculator) >>

    /// <summary>
    /// A rule dispatched by a master table's WageType reference rather than by wage code -- legacy's second
    /// dispatch mechanism (legacy <c>Program.cs</c> lines 2698-2701).
    /// </summary>
    private sealed class ClaimingWageTypeCalculator : IMasterDataDispatchedWageTypeCalculator
    {
        private readonly int _claimedWageTypeId;

        public ClaimingWageTypeCalculator(int claimedWageTypeId)
        {
            _claimedWageTypeId = claimedWageTypeId;
        }

        public string WageCode => null;

        public int CallCount { get; private set; }

        public bool ClaimsWageType(int wageTypeId) => wageTypeId == _claimedWageTypeId;

        public Task CalculateAsync(PayrollCalculationContext context)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The regression that wage code 2001 needed: a wage type NO calculator claims by code must still be
    /// calculated when a master table points at it. Before wave P9 the engine's membership test was
    /// <c>Resolve(code) != null</c>, which answered "no" here and silently produced nothing.
    /// </summary>
    [Fact]
    public async Task A_MasterData_Claimant_Should_Run_For_A_WageCode_No_Rule_Claims()
    {
        var loanRecovery = new ClaimingWageTypeCalculator(claimedWageTypeId: 1000);
        var resolver = BuildResolver(new IWageTypeCalculator[] { loanRecovery });

        // "2001" is deliberately claimed by NO calculator's WageCode -- exactly as legacy leaves it
        // (legacy Program.cs line 2691 comments "2001" out of wtcalList).
        var wageType = Wage(1000, "2001", BillingType.Deduction);

        resolver.Resolve("2001").ShouldBeNull();
        resolver.HasCalculatorFor(wageType).ShouldBeTrue();

        (await resolver.TryCalculateAsync(Context(wageType))).ShouldBeTrue();
        loanRecovery.CallCount.ShouldBe(1);
    }

    [Fact]
    public void HasCalculatorFor_Should_Be_False_When_Neither_Code_Nor_Master_Table_Claims_It()
    {
        var resolver = BuildResolver(new IWageTypeCalculator[]
        {
            new RecordingWageTypeCalculator("1006"),
            new ClaimingWageTypeCalculator(claimedWageTypeId: 1000)
        });

        resolver.HasCalculatorFor(Wage(4711, "4711")).ShouldBeFalse();
    }

    /// <summary>
    /// Master-data claimants must never be reachable by code. They declare <c>WageCode == null</c>, and
    /// several of them will coexist once Air Ticket Pay / Travel Per Diem / Violation Deduction land -- so
    /// <see cref="WageTypeCalculatorResolver.Resolve"/> must not see them as "duplicate claims on null".
    /// </summary>
    [Fact]
    public void Resolve_Should_Ignore_MasterData_Claimants_Entirely()
    {
        var resolver = BuildResolver(new IWageTypeCalculator[]
        {
            new ClaimingWageTypeCalculator(claimedWageTypeId: 1000),
            new ClaimingWageTypeCalculator(claimedWageTypeId: 1001),
            new RecordingWageTypeCalculator("1006")
        });

        Should.NotThrow(() => resolver.Resolve("1006"));
        resolver.Resolve("1006").ShouldNotBeNull();
    }

    /// <summary>
    /// Legacy's master-table regions are sequential blocks, not an else-if chain, so EVERY claimant runs --
    /// and they run BEFORE the by-code rule (legacy <c>EmployeeCalculatedPayService.Main</c> lines 44-130
    /// precede the code branches from line 131).
    /// </summary>
    [Fact]
    public async Task All_Claimants_Run_And_They_Run_Before_The_ByCode_Rule()
    {
        var first = new ClaimingWageTypeCalculator(claimedWageTypeId: 1000);
        var second = new ClaimingWageTypeCalculator(claimedWageTypeId: 1000);
        var byCode = new RecordingWageTypeCalculator("2001");

        var resolver = BuildResolver(new IWageTypeCalculator[] { first, second, byCode });

        (await resolver.TryCalculateAsync(Context(Wage(1000, "2001", BillingType.Deduction)))).ShouldBeTrue();

        first.CallCount.ShouldBe(1);
        second.CallCount.ShouldBe(1);
        byCode.CallCount.ShouldBe(1);
    }

    /// <summary>The four dispatcher gates apply to master-data claimants too, not just to by-code rules.</summary>
    [Fact]
    public async Task Gates_Should_Apply_To_MasterData_Claimants()
    {
        var claimant = new ClaimingWageTypeCalculator(claimedWageTypeId: 1000);
        var resolver = BuildResolver(new IWageTypeCalculator[] { claimant });

        var context = Context(
            Wage(1000, "2001", BillingType.Deduction),
            onHold: new PayrollOnHoldFlags { AllDeductions = true });

        (await resolver.TryCalculateAsync(context)).ShouldBeFalse();
        claimant.CallCount.ShouldBe(0);
    }

    #endregion
}
