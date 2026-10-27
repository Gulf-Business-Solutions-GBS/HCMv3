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
/// Wave P4 -- <see cref="EmployeeLeavePayCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeLeavePayService.Main</c>/<c>GetLeavePay</c>.
/// </summary>
public class EmployeeLeavePayCalculationServiceTests
{
    private const int EmployeeId = 7;
    private const int RegisterId = 500;

    private const int BasicPayWageTypeId = 50;   // code 1000
    private const int LeavePayWageTypeId = 300;  // the leave type's wage type
    private const int EncashWageTypeId = 301;    // code 1052
    private const int AnnualLeaveAbsenceTypeId = 201;
    private const int LeaveAbsenceTypeId = 200;

    private static readonly DateTime PayDate = new DateTime(2025, 3, 31); // 31 calendar days

    #region << Harness >>

    /// <summary>Nested for the same concurrency reason as the basic-pay test's copy.</summary>
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

    private readonly RecordingAdvancePaymentService AdvancePaymentService = new RecordingAdvancePaymentService();

    private readonly PayrollRegisterStoreFake _store = new PayrollRegisterStoreFake();

    private EmployeeLeavePayCalculationService Build(
        IEnumerable<AbsenceType> absenceTypes = null,
        IEnumerable<WageType> wageTypes = null,
        IEnumerable<WageTypeLeave> wageTypeLeaves = null,
        IEnumerable<EmployeeAbsence> absences = null,
        IEnumerable<EmployeeAbsAdjustment> adjustments = null)
    {
        return new EmployeeLeavePayCalculationService(
            Repo(absenceTypes ?? Enumerable.Empty<AbsenceType>()),
            Repo(wageTypes ?? Enumerable.Empty<WageType>()),
            Repo(wageTypeLeaves ?? Enumerable.Empty<WageTypeLeave>()),
            Repo(absences ?? Enumerable.Empty<EmployeeAbsence>()),
            Repo(adjustments ?? Enumerable.Empty<EmployeeAbsAdjustment>()),
            Repo(Enumerable.Empty<EmployeeSubGroupAbsence>()),
            Repo(Enumerable.Empty<AbsenceDedRuleDetail>()),
            Repo(Enumerable.Empty<EmployeeDutyPeriod>()),
            Repo(Enumerable.Empty<TimeRegister>()),
            _store,
            Substitute.For<IPayrollDayCountService>(),
            Substitute.For<IAccrualService>(),
            Substitute.For<IRosterService>(),
            SubGroupResolver(),
            AdvancePaymentService);
    }

    /// <summary>
    /// Stands in for legacy's <c>emp.OrgAssignment</c> sub-group lookup, now backed by
    /// <c>EmployeeOrgAssignment.EmployeeSubGroupId</c>.
    ///
    /// <para>The only place the service consults the sub-group is the unpaid-sick-leave deduction rule, which
    /// then reads <c>EmployeeSubGroupAbsence</c> -- and <see cref="Build"/> always hands that repository an
    /// EMPTY list, so the rule short-circuits and returns <c>leaveDays</c> unchanged either way. A resolver
    /// returning <c>null</c> therefore reproduces exactly what these tests asserted before the FK landed
    /// (legacy's own <c>empSubGroup == null</c> arm). No expected amount in this file changes.</para>
    /// </summary>
    private static IEmployeeSubGroupResolver SubGroupResolver(int? employeeSubGroupId = null)
    {
        var resolver = Substitute.For<IEmployeeSubGroupResolver>();
        resolver.GetEmployeeSubGroupIdAsync(default, default)
            .ReturnsForAnyArgs(Task.FromResult(employeeSubGroupId));

        return resolver;
    }

    private static WageType Wage(
        int id,
        string code,
        BillingMethod billingMethod = BillingMethod.CalendarDaysPerMonth,
        double fixedDays = 26,
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
            FixedDays = fixedDays,
            IncludeInAdvancePay = includeInAdvancePay,
            Status = true
        };
    }

    private static AbsenceType Absence(int id, string code, int? wageTypeId, QuotaType quotaType = QuotaType.None)
    {
        return new AbsenceType
        {
            Id = id,
            TenantId = 1,
            Code = code,
            NameEn = "Absence " + code,
            NameAr = "Absence " + code,
            WageTypeId = wageTypeId,
            QuotaType = quotaType,
            IsActive = true
        };
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        PayrollRunType runType = PayrollRunType.Regular,
        List<TimeRegister> timeRegisters = null)
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
            TimeRegisterList = timeRegisters,
            TimeRegisterQuery = (timeRegisters ?? new List<TimeRegister>()).AsQueryable(),
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayDate,
                InPeriod = PayDate,
                PayrollRunType = runType
            }
        };
    }

    private static TimeRegister LeaveDay(DateTime date, int leaveTypeId, double leaveDay = 1,
        double includeInLeavePay = 0, double leaveAdjDay = 0)
    {
        return new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            DayDate = date,
            LeaveTypeId = leaveTypeId,
            LeaveDay = leaveDay,
            IncludeInLeavePay = includeInLeavePay,
            LeaveAdjDay = leaveAdjDay
        };
    }

    private List<TimeRegister> ThreeLeaveDays()
    {
        return new List<TimeRegister>
        {
            LeaveDay(new DateTime(2025, 3, 10), LeaveAbsenceTypeId),
            LeaveDay(new DateTime(2025, 3, 11), LeaveAbsenceTypeId),
            LeaveDay(new DateTime(2025, 3, 12), LeaveAbsenceTypeId)
        };
    }

    #endregion

    #region << GetLeavePayWageTypeIdsAsync (legacy Program.cs lines 2675-2676) >>

    [Fact]
    public async Task GetLeavePayWageTypeIdsAsync_Should_Return_Every_AbsenceType_WageType_Plus_1052()
    {
        var service = Build(
            absenceTypes: new[]
            {
                Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId),
                Absence(202, "11", null) // no wage type -> excluded (legacy Program.cs line 2666)
            },
            wageTypes: new[] { Wage(EncashWageTypeId, "1052") });

        var ids = await service.GetLeavePayWageTypeIdsAsync();

        ids.ShouldBe(new[] { LeavePayWageTypeId, EncashWageTypeId });
    }

    #endregion

    #region << Main -- leave types (legacy EmployeeLeavePayService.cs lines 42-285) >>

    /// <summary>Legacy lines 30-39: the whole body is skipped when the employee has not returned from leave.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Do_Nothing_When_NotReturnFromLeave()
    {
        var days = ThreeLeaveDays();
        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { Wage(LeavePayWageTypeId, "2000") });

        var context = Context(Wage(LeavePayWageTypeId, "2000"), timeRegisters: days);
        context.IsNotReturnFromLeave = true;

        await service.CalculateAsync(context);

        _store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 373-396 + 457-480: Rate is the SUM of the Rates of every wage type flagged
    /// <c>WageTypeLeave</c> for this absence type, and the amount is rate/calendarDays * leaveDays.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Value_Leave_Days_At_The_IncludeInPaidLeave_Rate_Sum()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });
        _store.Seed(new PayrollRegisterLine { Id = 2, WageTypeId = 51, WageCode = "1001", Rate = 62 });

        var leaveWage = Wage(LeavePayWageTypeId, "2000");
        var days = ThreeLeaveDays();

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = LeaveAbsenceTypeId },
                new WageTypeLeave { Id = 2, TenantId = 1, WageTypeId = 51, AbsenceTypeId = LeaveAbsenceTypeId }
            });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: days));

        var line = _store.Lines.Single(l => l.WageTypeId == LeavePayWageTypeId);
        line.Rate.ShouldBe(372);                 // 310 + 62
        line.Number.ShouldBe(3);
        line.Amount.ShouldBe(372d / 31 * 3);
    }

    /// <summary>Legacy lines 59-70 and 419-430: IncludeInLeavePay days are ADDED to the leave-day count.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Add_IncludeInLeavePay_Days_To_The_Leave_Day_Count()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });

        var leaveWage = Wage(LeavePayWageTypeId, "2000");
        var days = new List<TimeRegister>
        {
            LeaveDay(new DateTime(2025, 3, 10), LeaveAbsenceTypeId, includeInLeavePay: 1),
            LeaveDay(new DateTime(2025, 3, 11), LeaveAbsenceTypeId)
        };

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = LeaveAbsenceTypeId }
            });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: days));

        _store.Lines.Single(l => l.WageTypeId == LeavePayWageTypeId).Number.ShouldBe(3); // 2 leave + 1 include
    }

    /// <summary>Legacy lines 72-83 and 432-443: LeaveAdjDay is added too (full-month leave to fixed days).</summary>
    [Fact]
    public async Task CalculateAsync_Should_Add_LeaveAdjDay_To_The_Leave_Day_Count()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });

        var leaveWage = Wage(LeavePayWageTypeId, "2000");
        var days = new List<TimeRegister>
        {
            LeaveDay(new DateTime(2025, 3, 10), LeaveAbsenceTypeId, leaveAdjDay: 2)
        };

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = LeaveAbsenceTypeId }
            });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: days));

        _store.Lines.Single(l => l.WageTypeId == LeavePayWageTypeId).Number.ShouldBe(3); // 1 + 2
    }

    /// <summary>Legacy lines 457-461: FixedDaysPerMonth divides by the LEAVE wage type's FixedDays.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Divide_By_FixedDays_For_FixedDaysPerMonth()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 260 });

        var leaveWage = Wage(LeavePayWageTypeId, "2000", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        var days = ThreeLeaveDays();

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = LeaveAbsenceTypeId }
            });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: days));

        _store.Lines.Single(l => l.WageTypeId == LeavePayWageTypeId).Amount.ShouldBe(260d / 26 * 3);
    }

    /// <summary>
    /// Legacy line 281: the leave-pay line is added UNCONDITIONALLY -- no "zero means delete", unlike the
    /// 1052 encashment branch below.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Write_The_Leave_Line_Even_When_The_Amount_Is_Zero()
    {
        // No WageTypeLeave rows -> totalBasicPay is 0 -> amount is 0.
        var leaveWage = Wage(LeavePayWageTypeId, "2000");
        var days = ThreeLeaveDays();

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: days));

        var line = _store.Lines.Single(l => l.WageTypeId == LeavePayWageTypeId);
        line.Amount.ShouldBe(0);
        line.Number.ShouldBe(3);
        _store.DeletedLineIds.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 247-248: nothing is written when there are no leave days at all.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Write_Nothing_When_There_Are_No_Leave_Days()
    {
        var leaveWage = Wage(LeavePayWageTypeId, "2000");

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage });

        await service.CalculateAsync(Context(leaveWage, timeRegisters: new List<TimeRegister>()));

        _store.Lines.ShouldBeEmpty();
    }

    /// <summary>Legacy lines 219-243: advance-paid absences are flagged <c>AdvanceLeavePaid</c>.</summary>
    [Fact]
    public async Task CalculateAsync_Should_Flag_AdvancePaid_Absences_On_An_OffCycle_Run()
    {
        var absence = new EmployeeAbsence
        {
            Id = 1,
            TenantId = 1,
            EmployeeId = EmployeeId,
            AbsenceTypeId = LeaveAbsenceTypeId,
            LeavePaymentType = LeavePaymentType.PayInAdvance,
            PaymentDate = default,
            StartDate = new DateTime(2025, 3, 10),
            EndDate = new DateTime(2025, 3, 12),
            AdvanceLeavePaid = false
        };

        var leaveWage = Wage(LeavePayWageTypeId, "2000", includeInAdvancePay: true);

        var service = Build(
            absenceTypes: new[] { Absence(LeaveAbsenceTypeId, "10", LeavePayWageTypeId) },
            wageTypes: new[] { leaveWage },
            absences: new[] { absence });

        var context = Context(leaveWage, PayrollRunType.OffCycle, ThreeLeaveDays());
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await service.CalculateAsync(context);

        absence.AdvanceLeavePaid.ShouldBeTrue();
    }

    #endregion

    #region << Main -- wage code 1052 Leave Encashment (legacy lines 289-352) >>

    private EmployeeAbsAdjustment Adjustment(
        double quota,
        AdjustmentType type = AdjustmentType.Encashment,
        AdjustmentPaymentType paymentType = AdjustmentPaymentType.PayWithPayroll)
    {
        return new EmployeeAbsAdjustment
        {
            Id = 1,
            TenantId = 1,
            EmployeeId = EmployeeId,
            AbsenceTypeId = AnnualLeaveAbsenceTypeId,
            AdjustmentType = type,
            AdjustmentPaymentType = paymentType,
            EffectiveDate = new DateTime(2025, 3, 15),
            QuotaAdjusted = quota
        };
    }

    [Fact]
    public async Task CalculateAsync_Should_Pay_Leave_Encashment_For_WageCode_1052()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });

        var encashWage = Wage(EncashWageTypeId, "1052");

        var service = Build(
            // AbsenceType "1" points at a DIFFERENT wage type, so the leave-type loop finds nothing and the
            // 1052 branch is taken -- exactly legacy's else-if.
            absenceTypes: new[] { Absence(AnnualLeaveAbsenceTypeId, "1", LeavePayWageTypeId, QuotaType.AnnualLeave) },
            wageTypes: new[] { encashWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId }
            },
            adjustments: new[] { Adjustment(5) });

        await service.CalculateAsync(Context(encashWage));

        var line = _store.Lines.Single(l => l.WageTypeId == EncashWageTypeId);
        line.Rate.ShouldBe(310);
        line.Number.ShouldBe(5);
        line.Amount.ShouldBe(310d / 31 * 5);
    }

    /// <summary>
    /// Legacy lines 344-345: the 1052 line IS deleted when its amount lands on zero -- unlike the leave-pay
    /// branch above, which writes unconditionally.
    ///
    /// <para>NOTE the pre-existing line is seeded with <c>Amount = 0</c>: legacy's <c>GetLeavePay</c> ends with
    /// <c>salaryRegisterDetail.Amount += totalleaveamount</c> (legacy line 824), so a line that already
    /// carries a non-zero amount can never be zeroed by a zero contribution.</para>
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Delete_The_1052_Line_When_The_Amount_Is_Zero()
    {
        _store.Seed(new PayrollRegisterLine { Id = 77, WageTypeId = EncashWageTypeId, WageCode = "1052", Amount = 0 });

        var encashWage = Wage(EncashWageTypeId, "1052");

        var service = Build(
            absenceTypes: new[] { Absence(AnnualLeaveAbsenceTypeId, "1", LeavePayWageTypeId, QuotaType.AnnualLeave) },
            wageTypes: new[] { encashWage },
            // No WageTypeLeave -> totalBasicPay 0 -> amount 0.
            adjustments: new[] { Adjustment(5) });

        await service.CalculateAsync(Context(encashWage));

        _store.Lines.ShouldBeEmpty();
        _store.DeletedLineIds.ShouldContain(77);
    }

    [Fact]
    public async Task CalculateAsync_Should_Write_Nothing_For_1052_When_There_Are_No_Encashment_Adjustments()
    {
        var encashWage = Wage(EncashWageTypeId, "1052");

        var service = Build(
            absenceTypes: new[] { Absence(AnnualLeaveAbsenceTypeId, "1", LeavePayWageTypeId, QuotaType.AnnualLeave) },
            wageTypes: new[] { encashWage });

        await service.CalculateAsync(Context(encashWage));

        _store.Lines.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 383-386: with <c>regularLeaveEncashOnBasicPayOnly</c> the encashment rate collapses to
    /// the 1000 line's Rate alone, ignoring the other IncludeInPaidLeave wage types.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Encash_On_BasicPay_Only_When_The_Setting_Is_On()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });
        _store.Seed(new PayrollRegisterLine { Id = 2, WageTypeId = 51, WageCode = "1001", Rate = 62 });

        var encashWage = Wage(EncashWageTypeId, "1052");

        var service = Build(
            absenceTypes: new[] { Absence(AnnualLeaveAbsenceTypeId, "1", LeavePayWageTypeId, QuotaType.AnnualLeave) },
            wageTypes: new[] { encashWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId },
                new WageTypeLeave { Id = 2, TenantId = 1, WageTypeId = 51, AbsenceTypeId = AnnualLeaveAbsenceTypeId }
            },
            adjustments: new[] { Adjustment(5) });

        var context = Context(encashWage);
        context.Settings.RegularLeaveEncashOnBasicPayOnly = true;

        await service.CalculateAsync(context);

        _store.Lines.Single(l => l.WageTypeId == EncashWageTypeId).Rate.ShouldBe(310);
    }

    /// <summary>
    /// Legacy lines 302-312: on an advance-leave/leave-encashment off-cycle for the off-cycle period itself,
    /// ONLY <c>PayInAdvance</c> adjustments count.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_Should_Count_Only_PayInAdvance_Adjustments_On_An_AdvanceLeave_OffCycle()
    {
        _store.Seed(new PayrollRegisterLine { Id = 1, WageTypeId = BasicPayWageTypeId, WageCode = "1000", Rate = 310 });

        var encashWage = Wage(EncashWageTypeId, "1052");

        var service = Build(
            absenceTypes: new[] { Absence(AnnualLeaveAbsenceTypeId, "1", LeavePayWageTypeId, QuotaType.AnnualLeave) },
            wageTypes: new[] { encashWage },
            wageTypeLeaves: new[]
            {
                new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicPayWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId }
            },
            adjustments: new[] { Adjustment(5, paymentType: AdjustmentPaymentType.PayWithPayroll) });

        var context = Context(encashWage, PayrollRunType.OffCycle);
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;

        await service.CalculateAsync(context);

        // The single adjustment is PayWithPayroll, so it is filtered out and nothing is written.
        _store.Lines.Count(l => l.WageTypeId == EncashWageTypeId).ShouldBe(0);
    }

    #endregion
}
