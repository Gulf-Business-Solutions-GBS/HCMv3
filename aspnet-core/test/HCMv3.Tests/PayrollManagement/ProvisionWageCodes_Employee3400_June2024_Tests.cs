using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Legacy-parity regression for the four <b>3xxx provision</b> wage codes, pinned to the production figures
/// for employee <b>3400</b>, period <b>2024-06-30</b>, taken from the legacy <c>HRMS_RCF</c> database at
/// <c>MAX(InPeriod)</c> for that <c>ForPeriod</c>:
///
/// <list type="table">
/// <item><description><c>3000</c> Indemnity Provision -- Rate 185, Number 2.065753, Amount 14.698628</description></item>
/// <item><description><c>3001</c> Annual Leave Provision -- Rate 185, Number 2.459010, Amount 17.496802</description></item>
/// <item><description><c>3003</c> Total Indemnity Provision -- Rate 185, Number 368.978082, Amount 2625.420969</description></item>
/// <item><description><c>3004</c> Total Annual Leave Provision -- Rate 185, Number 16.374005, Amount 116.507343</description></item>
/// </list>
///
/// <para>Before this wave the engine produced <b>none</b> of the four: every line was created as a zero
/// placeholder and then dropped by the zero-means-delete. Three independent causes, all outside the provision
/// arithmetic itself:</para>
///
/// <list type="number">
/// <item><description><b>3001 / 3004 -- the <c>WageTypeLeaves</c> table was empty</b> (0 rows against
/// legacy's 230); it was missing from the legacy migrator's plan entirely. It is the sole input to legacy's
/// <c>includeInPaidLeaveWageTypes</c> (<c>EmployeeCalculatedPayService.cs</c> lines 2290 and 2328), so the
/// <c>totalBasicPay</c> Rate sum ran over an empty set and both amounts came out 0. No calculation
/// change was needed for these two -- their day counts (4010 / 4022) already matched legacy exactly.</description></item>
/// <item><description><b>3000 / 3003 -- the slab table was never built on a Regular run.</b> Legacy read the
/// static <c>Program.IndemnitySlabList</c>, populated as a side effect of <c>Program.GetTotalIndemnityDays</c>
/// (<c>Program.cs</c> line 3391, <c>AddIndemnityRecord</c> at lines 3648/3662) and cleared once per employee
/// by <c>CreateIndemnityTable()</c> (line 1914). This port has no static and passed <c>indemnitySlabs: null</c>,
/// so the slab loop in <c>GetIndemnityBifercateAmount</c> (line 2699) was skipped and the amount was 0.</description></item>
/// <item><description><b>3003 -- the indemnity day count was 17 days short.</b>
/// <c>GetTotalUnpaidLeaveDays</c> (<c>Program.cs</c> line 3701) reads TWO different absence types: the
/// <c>EmployeeAbsAdjustment</c> term uses <c>UnpaidLeaveOid</c> (code "2", line 3739) but the
/// <c>TimeRegister.LeaveDay</c> term uses the hard-coded <c>LeaveType.Code == "15"</c> (lines 3734-3737).
/// The port passed the code-"2" id to both, double-counting 17 unpaid-leave days that
/// <c>UnpaidLeaveDay</c> had already counted.</description></item>
/// </list>
///
/// <para><b>3000 additionally requires the PRIOR month's register to exist.</b> It is a month-over-month
/// delta: legacy <c>GetIndemnityDays</c> (<c>Program.cs</c> line 3760) computes
/// <c>4025(current) - 4025(previous month)</c>, and when no previous register is found at all it treats the
/// run as the employee's first and subtracts nothing (line 3792). Validating 3000 from a single-month run
/// therefore yields the running total, not the monthly accrual -- a property of the run, not a defect. See
/// <see cref="Wage3000_Is_A_MonthOverMonth_Delta_Of_The_4025_Closing_Balance"/>.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProvisionWageCodes_Employee3400_June2024_Tests : AppTestBase
{
    private readonly IIndemnityCalculationService _indemnityService;

    public ProvisionWageCodes_Employee3400_June2024_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _indemnityService = Resolve<IIndemnityCalculationService>();

        UsingDbContext(context =>
        {
            if (context.Set<ManagePayrollSettings>().FirstOrDefault() == null)
            {
                context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    EnableUnpaidDedSlabWise = true
                });
            }
        });
    }

    /// <summary>Sum of the Rate of every <c>IncludeInIndemnity</c> register line -- wage code 1000 alone.</summary>
    private const double TotalBasicPay = 185d;

    /// <summary><c>WageType.FixedDays</c> for all four provision wage types (verified in both databases).</summary>
    private const double FixedDays = 26d;

    private static readonly DateTime PayDate = new DateTime(2024, 6, 30);

    /// <summary>
    /// The employee's real indemnity slab table at 2024-06-30, as produced by
    /// <c>IAccrualService.GetTotalIndemnityTableAsync</c> (legacy <c>Program.GetSlabWiseIndemnityTable</c>,
    /// line 3996) from contract start 2007-07-04 and the sub-group gratuity rows.
    ///
    /// <para>Slab 2's 255 unpaid days is the figure legacy produces and is derived, not asserted: 228 from
    /// <c>TimeTransfer</c> type "5" + 22 <c>UnpaidLeaveDay</c> + 5 <c>LeaveDay</c> on absence code "15".
    /// Slab totals: 75 + 293.978082 = 368.978082, which is legacy's wage code 4006/4025 Number exactly.</para>
    /// </summary>
    private static List<IndemnitySlab> Employee3400Slabs()
    {
        return new List<IndemnitySlab>
        {
            new IndemnitySlab
            {
                SrNo = 1,
                StartDate = new DateTime(2007, 7, 4),
                EndDate = new DateTime(2012, 7, 3),
                TotalDays = 1825,
                UnpaidDays = 0,
                ActualDays = 1825,
                IndemnityRate = 15,
                IndemnityDays = 75,
                BasisDays = 26
            },
            new IndemnitySlab
            {
                SrNo = 2,
                StartDate = new DateTime(2012, 7, 4),
                EndDate = new DateTime(2024, 6, 30),
                TotalDays = 4382,
                UnpaidDays = 255,
                ActualDays = 4127,
                IndemnityRate = 26,
                IndemnityDays = 26d / 365d * 4127d,
                BasisDays = 26
            }
        };
    }

    #region << 3003 -- Total Indemnity Provision >>

    /// <summary>
    /// Wage code <b>3003</b>. Legacy <c>GetTotalIndemnityProvision</c> (line 2643) posts
    /// <c>Number = "4006".Number</c> and bifurcates it across the slabs with <c>IsMonthly = false</c>
    /// (line 2702), consuming the days in <c>SrNo</c> order.
    /// </summary>
    [Fact]
    public async Task Wage3003_Total_Indemnity_Provision_Should_Match_Legacy_2625_42()
    {
        var slabs = Employee3400Slabs();
        var totalIndemnityDays = slabs.Sum(s => s.IndemnityDays);

        // This is legacy's wage code 4006 / 4025 Number for 3400 at 2024-06-30.
        totalIndemnityDays.ShouldBe(368.978082, 1e-6);
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId: 1,
            payDate: PayDate,
            indemnityDays: totalIndemnityDays,
            totalBasicPay: TotalBasicPay,
            fixedDays: FixedDays,
            payrollRunType: PayrollRunType.Regular,
            offCycleReason: null,
            isMonthly: false,
            employeeSubGroupId: null,
            indemnitySlabs: slabs);

        result.Amount.ShouldBe(2625.420969, 1e-6);
    }

    #endregion

    #region << 3000 -- Indemnity Provision (monthly) >>

    /// <summary>
    /// Wage code <b>3000</b>. Legacy <c>GetIndemnityProvision</c> (line 2610) posts
    /// <c>Number = "4004".Number</c> -- the MONTH's accrual, 2.065753 -- and bifurcates with
    /// <c>IsMonthly = true</c> (line 2718), which charges the whole number to the single slab whose
    /// <c>[StartDate, EndDate]</c> contains <c>payDate</c> and deliberately ignores that slab's own day cap
    /// (legacy commented the cap out at line 2730).
    /// </summary>
    [Fact]
    public async Task Wage3000_Indemnity_Provision_Should_Match_Legacy_14_70()
    {
        var result = await _indemnityService.GetIndemnityBifurcateAmountAsync(
            employeeId: 1,
            payDate: PayDate,
            indemnityDays: 2.065753,
            totalBasicPay: TotalBasicPay,
            fixedDays: FixedDays,
            payrollRunType: PayrollRunType.Regular,
            offCycleReason: null,
            isMonthly: true,
            employeeSubGroupId: null,
            indemnitySlabs: Employee3400Slabs());

        result.Amount.ShouldBe(14.698628, 1e-6);
    }

    /// <summary>
    /// Pins the reason 3000 cannot be validated from a single-month run: it is
    /// <c>4025(current) - 4025(prior month)</c> (legacy <c>GetIndemnityDays</c>, Program.cs lines 3770-3801).
    /// The June closing balance is the running total; only the delta against May is the monthly accrual.
    /// Both closing balances below are the legacy figures for employee 3400.
    /// </summary>
    [Fact]
    public void Wage3000_Is_A_MonthOverMonth_Delta_Of_The_4025_Closing_Balance()
    {
        const double mayClosing4025 = 366.912329;
        const double juneClosing4025 = 368.978082;

        (juneClosing4025 - mayClosing4025).ShouldBe(2.065753, 1e-6);
    }

    #endregion

    #region << 3001 / 3004 -- Annual Leave Provisions >>

    /// <summary>
    /// Wage codes <b>3001</b> and <b>3004</b>. Legacy <c>GetAnnualLeaveProvision</c> (line 2298) and
    /// <c>GetTotalAnnualLeaveProvision</c> (line 2336) share one formula:
    /// <c>(sum of Rate over WageTypeLeave-linked annual-leave wage types / FixedDays) * Number</c>,
    /// where Number is wage code 4010 for 3001 and 4022 for 3004.
    ///
    /// <para>Both were 0 purely because <c>WageTypeLeaves</c> held no rows, so the Rate sum was empty. With
    /// wage code 1000 linked (Rate 185) the legacy amounts follow exactly.</para>
    /// </summary>
    [Theory]
    // 3001: Number comes from wage code 4010.
    [InlineData("3001", "4010", 2.459010, 17.496802)]
    // 3004: Number comes from wage code 4022.
    [InlineData("3004", "4022", 16.374005, 116.507343)]
    public async Task AnnualLeaveProvisions_Should_Match_Legacy(string provisionCode, string sourceCode, double number, double expectedAmount)
    {
        const int basicWageTypeId = 101;
        const int annualLeaveAbsenceTypeId = 1;

        var store = new FakePayrollRegisterStore();

        // Wage code 1000, the only IncludeInIndemnity/annual-leave-linked earning on 3400's June register.
        store.Lines.Add(new PayrollRegisterLine { Id = 11, WageTypeId = basicWageTypeId, WageCode = "1000", Rate = TotalBasicPay, Amount = 178.833333 });
        store.Lines.Add(new PayrollRegisterLine { Id = 12, WageTypeId = 500, WageCode = sourceCode, Number = number });

        var service = new AnnualLeaveProvisionService(
            store,
            Repo(new List<SalaryRegister>()),
            // THE regression guard: with this table empty -- as the migrator left it -- the Rate sum is over
            // an empty set, Amount is 0, and the line is dropped by the zero-means-delete.
            Repo(new[] { new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = basicWageTypeId, AbsenceTypeId = annualLeaveAbsenceTypeId } }),
            Repo(new[] { new AbsenceType { Id = annualLeaveAbsenceTypeId, TenantId = 1, Code = "1" } }),
            Repo(new List<Employee>()),
            Repo(new List<EmployeeBasicPay>()),
            Repo(new List<EmployeeBasicPayDetail>()),
            Repo(new List<EmployeeAbsAdjustment>()),
            Repo(new List<HCMv3.TimeManagement.TimeRegister>()));

        var context = new PayrollCalculationContext
        {
            EmployeeId = 7,
            PayDate = PayDate,
            PayStartDate = new DateTime(2024, 6, 1),
            PayInDate = PayDate,
            PayrollRunType = PayrollRunType.Regular,
            WageType = new WageType
            {
                Id = 31,
                TenantId = 1,
                Code = provisionCode,
                NameEn = provisionCode,
                FixedDays = FixedDays,
                BillingType = BillingType.Provision,
                Status = true
            },
            Register = new PayrollRegisterRef
            {
                RegisterId = 900,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayDate,
                InPeriod = PayDate,
                PayrollRunType = PayrollRunType.Regular
            },
            Settings = new PayrollEngineSettings
            {
                IsPeriodwiseProvision = false,
                AnnualLeaveAbsenceTypeId = annualLeaveAbsenceTypeId
            }
        };

        var result = provisionCode == "3001"
            ? await service.GetAnnualLeaveProvisionAsync(context)
            : await service.GetTotalAnnualLeaveProvisionAsync(context);

        result.Rate.ShouldBe(TotalBasicPay);
        result.Number.ShouldBe(number, 1e-6);
        result.Amount.ShouldBe(expectedAmount, 1e-6);
    }

    #endregion
}
