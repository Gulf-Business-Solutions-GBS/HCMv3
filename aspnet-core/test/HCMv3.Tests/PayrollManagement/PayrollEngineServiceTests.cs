using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1b. Behaviour tests for <see cref="PayrollEngineService"/>, concentrated on the four behaviours most
/// likely to regress silently:
/// <list type="number">
/// <item><description>the two category-level on-hold gates the resolver does NOT apply (leave pay, payments)
/// and the INVERTED basic-pay one;</description></item>
/// <item><description><see cref="WageType.PayrollOrder"/> being honoured, and 9000 Net Pay therefore running
/// last;</description></item>
/// <item><description>per-employee error isolation;</description></item>
/// <item><description><see cref="PayrollRegisterTarget"/> being threaded through rather than
/// hardcoded.</description></item>
/// </list>
/// </summary>
public class PayrollEngineServiceTests
{
    private const string PayrollAreaName = "PA";
    private static readonly DateTime PayInDate = new DateTime(2024, 6, 30);

    #region << Ordering >>

    [Fact]
    public async Task RunAsync_Should_Iterate_WageTypes_In_PayrollOrder_With_NetPay_Last()
    {
        var harness = new Harness();

        // Deliberately inserted in a NON-PayrollOrder sequence, and 9000 is not last in the list.
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.WageTypes.Add(WageType("2000", "Late Early Deduction", 200, BillingType.Deduction));
        harness.WageTypes.Add(WageType("1006", "Loan Pay", 100, BillingType.Earning));
        harness.WageTypes.Add(WageType("0023", "Fund Deduction", 300, BillingType.Deduction));

        harness.CalculatedWageCodes.UnionWith(new[] { "9000", "2000", "1006", "0023" });

        await harness.Build().RunAsync(harness.RegularInput());

        harness.DispatchedWageCodes.ShouldBe(new[] { "1006", "2000", "0023", "9000" });
        harness.DispatchedWageCodes.Last().ShouldBe("9000");
    }

    #endregion

    #region << On-hold gates the resolver does NOT apply >>

    [Fact]
    public async Task RunAsync_Should_Skip_LeavePay_When_OnHold_LeavePay_Is_Set()
    {
        var harness = new Harness();
        var leaveWageType = WageType("1050", "Annual Leave Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(leaveWageType);
        harness.LeavePayWageTypeIds.Add(leaveWageType.Id);

        harness.OnHolds.Add(OnHold(onHoldLeavePay: true));

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.LeavePayService.DidNotReceive().CalculateAsync(Arg.Any<PayrollCalculationContext>());
    }

    [Fact]
    public async Task RunAsync_Should_Run_LeavePay_When_OnHold_LeavePay_Is_Not_Set()
    {
        var harness = new Harness();
        var leaveWageType = WageType("1050", "Annual Leave Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(leaveWageType);
        harness.LeavePayWageTypeIds.Add(leaveWageType.Id);

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.LeavePayService.Received(1).CalculateAsync(Arg.Any<PayrollCalculationContext>());
    }

    /// <summary>
    /// <c>onHold_BasicPay</c> works the OPPOSITE way to every other hold: legacy passes it INTO
    /// <c>EmployeeBasicPayService</c>, where it suppresses Number/Amount but STILL WRITES RATE (legacy
    /// <c>EmployeeBasicPayService.cs</c> lines 224-228). Gating basic pay out on it would drop the rate row.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Still_Dispatch_BasicPay_When_OnHold_BasicPay_Is_Set_And_Pass_The_Flag_Through()
    {
        var harness = new Harness();
        var basicWageType = WageType("1000", "Basic Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(basicWageType);
        harness.BasicPayWageTypeIds.Add(basicWageType.Id);

        harness.OnHolds.Add(OnHold(onHoldBasicPay: true));

        PayrollCalculationContext captured = null;
        harness.BasicPayService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => captured = c))
            .Returns(Task.CompletedTask);

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.BasicPayService.Received(1).CalculateAsync(Arg.Any<PayrollCalculationContext>());
        captured.ShouldNotBeNull();
        captured.OnHold.BasicPay.ShouldBeTrue();
    }

    /// <summary>
    /// Legacy line 2547's earning/deduction PAIR test -- NOT the resolver's generic <c>IsOnHold</c>.
    /// A Deduction payment must still run while only earnings are on hold.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Apply_The_Earning_Deduction_Pair_Gate_To_Payments()
    {
        var harness = new Harness();
        var earning = WageType("1100", "Bonus", 100, BillingType.Earning);
        var deduction = WageType("2100", "Fine", 200, BillingType.Deduction);
        harness.WageTypes.Add(earning);
        harness.WageTypes.Add(deduction);

        harness.Payments.Add(new EmployeePayment
        {
            Id = 1,
            EmployeeId = Harness.EmployeeId,
            WageTypeId = earning.Id,
            PaymentDate = new DateTime(2024, 6, 10)
        });
        harness.Payments.Add(new EmployeePayment
        {
            Id = 2,
            EmployeeId = Harness.EmployeeId,
            WageTypeId = deduction.Id,
            PaymentDate = new DateTime(2024, 6, 10)
        });

        harness.OnHolds.Add(OnHold(onHoldAllEarnings: true));

        var dispatched = new List<string>();
        harness.PaymentService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => dispatched.Add(c.WageType.Code)))
            .Returns(Task.CompletedTask);

        var engine = harness.Build();
        engine.PaymentService = harness.PaymentService;

        await engine.RunAsync(harness.RegularInput());

        dispatched.ShouldBe(new[] { "2100" });
    }

    #endregion

    #region << Per-employee error isolation >>

    [Fact]
    public async Task RunAsync_Should_Isolate_A_Failing_Employee_And_Continue_With_The_Next()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();

        var netPay = WageType("9000", "Net Pay", 900, BillingType.Deduction);
        harness.WageTypes.Add(netPay);
        harness.CalculatedWageCodes.Add("9000");

        // Employee 1 blows up inside the wage-type loop; employee 2 must still be calculated.
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        var result = await harness.Build().RunAsync(harness.RegularInput());

        result.EmployeeResults.Count.ShouldBe(2);

        var failed = result.EmployeeResults.Single(r => r.EmployeeId == Harness.EmployeeId);
        failed.Succeeded.ShouldBeFalse();

        var succeeded = result.EmployeeResults.Single(r => r.EmployeeId == Harness.SecondEmployeeId);
        succeeded.Succeeded.ShouldBeTrue();

        // One failed + one successful PayrollLog row.
        harness.PayrollLogs.Count(r => r.Status == 1).ShouldBe(1);
        harness.PayrollLogs.Count(r => r.Status == 0).ShouldBe(1);
    }

    /// <summary>Legacy lines 2594-2599: the wage code and name are appended to the logged message.</summary>
    [Fact]
    public async Task RunAsync_Should_Wrap_A_WageType_Failure_With_The_Legacy_Message()
    {
        var harness = new Harness();
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.CalculatedWageCodes.Add("9000");
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        var result = await harness.Build().RunAsync(harness.RegularInput());

        var failed = result.EmployeeResults.Single();
        failed.Succeeded.ShouldBeFalse();
        failed.Message.ShouldContain("Error in calculating WageType '9000 : Net Pay'");
        failed.Message.ShouldContain("boom");
    }

    [Fact]
    public async Task RunAsync_Should_Finalize_The_ExecutionLog_As_Partial_When_Some_Employees_Failed()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.CalculatedWageCodes.Add("9000");
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        await harness.Build().RunAsync(harness.RegularInput());

        var log = harness.ExecutionLogs.Single();
        log.Module.ShouldBe(ExecutionLogModule.PayrollEngine);
        log.TotalRecords.ShouldBe(2);
        log.Successfull.ShouldBe(1);
        log.Failed.ShouldBe(1);
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
    }

    /// <summary>Legacy lines 776-777: a <see cref="PayrollLog"/> row with StopFlag aborts the whole run.</summary>
    [Fact]
    public async Task RunAsync_Should_Abort_When_A_StopFlag_PayrollLog_Row_Exists()
    {
        var harness = new Harness();
        harness.PayrollLogs.Add(new PayrollLog { Id = 99, StopFlag = true });

        var result = await harness.Build().RunAsync(harness.RegularInput());

        result.Aborted.ShouldBeTrue();
        result.EmployeeResults.ShouldBeEmpty();
        harness.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    #endregion

    #region << Register creation / target threading >>

    [Fact]
    public async Task RunAsync_Should_Create_One_SalaryRegister_Header_Per_Period()
    {
        var harness = new Harness();

        var result = await harness.Build().RunAsync(harness.RegularInput());

        harness.SalaryRegisters.Count.ShouldBe(1);
        var register = harness.SalaryRegisters.Single();
        register.EmployeeId.ShouldBe(Harness.EmployeeId);
        register.ForPeriod.ShouldBe(PayInDate);
        register.InPeriod.ShouldBe(PayInDate);
        register.PayrollRunType.ShouldBe(PayrollRunType.Regular);
        register.PayrollResultType.ShouldBe(PayrollResultType.Current);

        result.EmployeeResults.Single().SalaryRegisterIds.ShouldContain(register.Id);
    }

    /// <summary>
    /// Wave P7 must stay a parameter flip: whatever <see cref="PayrollEngineRunInput.RegisterTarget"/> says
    /// has to reach <see cref="PayrollRegisterRef.Target"/> unchanged.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Thread_The_RegisterTarget_Through_To_The_Calculation_Context()
    {
        var harness = new Harness();
        harness.WageTypes.Add(WageType("1000", "Basic Pay", 100, BillingType.Earning));
        harness.BasicPayWageTypeIds.Add(harness.WageTypes.Last().Id);

        PayrollCalculationContext captured = null;
        harness.BasicPayService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => captured = c))
            .Returns(Task.CompletedTask);

        var input = harness.RegularInput();
        input.RegisterTarget = PayrollRegisterTarget.Live;

        await harness.Build().RunAsync(input);

        captured.ShouldNotBeNull();
        captured.Register.Target.ShouldBe(PayrollRegisterTarget.Live);
        captured.Register.RegisterId.ShouldBe(harness.SalaryRegisters.Single().Id);
    }

    #endregion

    #region << Run-level guards (these DO throw) >>

    [Fact]
    public async Task RunAsync_Should_Throw_When_No_PayInDate_Was_Given()
    {
        var harness = new Harness();
        var input = harness.RegularInput();
        input.PayInDate = default(DateTime);

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(() => harness.Build().RunAsync(input));
        ex.Message.ShouldBe("No Pay Date defined for Payroll Calculation");
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_The_Control_Record_Is_Not_Locked()
    {
        var harness = new Harness();
        harness.ControlRecord.PeriodLocked = false;

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(
            () => harness.Build().RunAsync(harness.RegularInput()));
        ex.Message.ShouldBe("Payroll Control Record not locked or is not current.");
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_The_Payroll_Calendar_Has_No_Row_For_The_Month()
    {
        var harness = new Harness();
        harness.PayrollCalendars.Clear();

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(
            () => harness.Build().RunAsync(harness.RegularInput()));
        ex.Message.ShouldBe("Payroll Calender does not have record for Payroll month you are running.");
    }

    #endregion

    #region << Not-ported category services fail loudly, per employee >>

    [Fact]
    public async Task RunAsync_Should_Fail_Only_That_Employee_When_An_Unported_Category_Service_Is_Reached()
    {
        var harness = new Harness();
        var arrearWageType = WageType("5000", "Basic Pay Arrear", 100, BillingType.Earning);
        arrearWageType.IncludeInArrearPay = true;
        harness.WageTypes.Add(arrearWageType);

        var result = await harness.Build().RunAsync(harness.RegularInput());

        var employeeResult = result.EmployeeResults.Single();
        employeeResult.Succeeded.ShouldBeFalse();
        employeeResult.Message.ShouldContain("PayrollArrearService");
        employeeResult.Message.ShouldContain("not ported");
    }

    #endregion

    #region << Fixtures >>

    private static int _nextWageTypeId = 1;

    private static WageType WageType(string code, string name, int payrollOrder, BillingType billingType)
    {
        return new WageType
        {
            Id = _nextWageTypeId++,
            Code = code,
            NameEn = name,
            NameAr = name,
            PayrollOrder = payrollOrder,
            BillingType = billingType,
            Status = true
        };
    }

    private static EmployeeOnHold OnHold(
        bool onHoldBasicPay = false,
        bool onHoldLeavePay = false,
        bool onHoldAllEarnings = false,
        bool onHoldAllDeductions = false)
    {
        return new EmployeeOnHold
        {
            Id = 1,
            EmployeeId = Harness.EmployeeId,
            StartDate = new DateTime(2000, 1, 1),
            EndDate = new DateTime(2099, 1, 1),
            OnHold = false,
            OnHoldBasicPay = onHoldBasicPay,
            OnHoldLeavePay = onHoldLeavePay,
            OnHoldAllEarnings = onHoldAllEarnings,
            OnHoldAllDeductions = onHoldAllDeductions
        };
    }

    /// <summary>
    /// Assembles a <see cref="PayrollEngineService"/> over in-memory repositories and substituted collaborators.
    /// Everything defaults to "one active employee, one unlocked-for-retro period, nothing on hold".
    /// </summary>
    private sealed class Harness
    {
        public const int EmployeeId = 1;
        public const int SecondEmployeeId = 2;
        public const int PayrollAreaId = 7;

        public readonly List<Employee> Employees = new List<Employee>();
        public readonly List<EmployeeOrgAssignment> OrgAssignments = new List<EmployeeOrgAssignment>();
        public readonly List<EmployeeContract> Contracts = new List<EmployeeContract>();
        public readonly List<EmployeePayrollStatus> PayrollStatuses = new List<EmployeePayrollStatus>();
        public readonly List<EmployeeOnHold> OnHolds = new List<EmployeeOnHold>();
        public readonly List<EmployeeAbsence> Absences = new List<EmployeeAbsence>();
        public readonly List<EmployeeAbsAdjustment> AbsAdjustments = new List<EmployeeAbsAdjustment>();
        public readonly List<EmployeeFnFPayment> FnFPayments = new List<EmployeeFnFPayment>();
        public readonly List<EmployeeSeparation> Separations = new List<EmployeeSeparation>();
        public readonly List<EmployeePayment> Payments = new List<EmployeePayment>();
        public readonly List<EmployeeRecurringPayment> RecurringPayments = new List<EmployeeRecurringPayment>();
        public readonly List<EmployeeOvertime> Overtimes = new List<EmployeeOvertime>();
        public readonly List<EmployeeAttendance> Attendances = new List<EmployeeAttendance>();
        public readonly List<EmployeeSubGroupPayment> SubGroupPayments = new List<EmployeeSubGroupPayment>();
        public readonly List<SalaryRegister> SalaryRegisters = new List<SalaryRegister>();
        public readonly List<PayrollCalendar> PayrollCalendars = new List<PayrollCalendar>();
        public readonly List<PayrollReasonType> ReasonTypes = new List<PayrollReasonType>();
        public readonly List<PayrollLog> PayrollLogs = new List<PayrollLog>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<WageTypeException> WageTypeExceptions = new List<WageTypeException>();
        public readonly List<TimeRegister> TimeRegisters = new List<TimeRegister>();
        public readonly List<ExecutionLog> ExecutionLogs = new List<ExecutionLog>();

        public readonly List<string> DispatchedWageCodes = new List<string>();
        public readonly HashSet<string> CalculatedWageCodes = new HashSet<string>();
        public readonly List<int> BasicPayWageTypeIds = new List<int>();
        public readonly List<int> LeavePayWageTypeIds = new List<int>();

        public int? ThrowForEmployeeId;

        public PayrollArea PayrollArea;
        public PayrollControlRecord ControlRecord;

        public readonly IEmployeeBasicPayCalculationService BasicPayService = Substitute.For<IEmployeeBasicPayCalculationService>();
        public readonly IEmployeeLeavePayCalculationService LeavePayService = Substitute.For<IEmployeeLeavePayCalculationService>();
        public readonly IEmployeePaymentCalculationService PaymentService = Substitute.For<IEmployeePaymentCalculationService>();

        public Harness()
        {
            PayrollArea = new PayrollArea { Id = PayrollAreaId, Code = "PA", Name = PayrollAreaName, CutOffDay = 0 };

            ControlRecord = new PayrollControlRecord
            {
                Id = 1,
                PayrollAreaId = PayrollAreaId,
                PayrollArea = PayrollArea,
                CalendarMonthEnd = PayInDate,
                CalendarMonthStart = new DateTime(2024, 6, 1),
                PeriodLocked = true,
                PeriodClosed = false
            };

            PayrollCalendars.Add(new PayrollCalendar
            {
                Id = 1,
                PayrollAreaId = PayrollAreaId,
                PayrollMonth = PayInDate.Month,
                PayrollYear = PayInDate.Year
            });

            AddEmployee(EmployeeId, "E1");
        }

        public void AddSecondEmployee() => AddEmployee(SecondEmployeeId, "E2");

        private void AddEmployee(int id, string code)
        {
            Employees.Add(new Employee
            {
                Id = id,
                Code = code,
                Status = EmployeeStatus.Active,
                JoiningDate = new DateTime(2020, 1, 1)
            });

            OrgAssignments.Add(new EmployeeOrgAssignment
            {
                Id = id,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 1, 1)
            });

            Contracts.Add(new EmployeeContract
            {
                Id = id,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 1, 1)
            });

            PayrollStatuses.Add(new EmployeePayrollStatus
            {
                Id = id,
                EmployeeId = id
            });
        }

        public PayrollEngineRunInput RegularInput()
        {
            return new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = PayInDate,
                PayrollAreaName = PayrollAreaName,
                RegisterTarget = PayrollRegisterTarget.Live
            };
        }

        public PayrollEngineService Build()
        {
            var settingsAccessor = Substitute.For<ISysSettingsAccessor>();
            settingsAccessor.GetPayrollSettingsAsync().Returns(new ManagePayrollSettings { DecimalInSalaryRegister = 6 });
            settingsAccessor.GetTimeSettingsAsync().Returns(new ManageTimeSettings());
            settingsAccessor.GetSecuritySettingsAsync().Returns(new ManageSecuritySettings());

            var areaResolver = Substitute.For<IEmployeePayrollAreaResolver>();
            areaResolver.GetEmployeeIdsInPayrollAreaAsync(Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(Employees.Select(e => e.Id).ToList()));
            areaResolver.GetEmployeePayrollAreaAsync(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(PayrollArea));

            BasicPayService.GetBasicPayWageTypeIdsAsync(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(BasicPayWageTypeIds.ToList()));
            LeavePayService.GetLeavePayWageTypeIdsAsync()
                .Returns(_ => Task.FromResult(LeavePayWageTypeIds.ToList()));

            var calculatorResolver = Substitute.For<IWageTypeCalculatorResolver>();
            calculatorResolver.Resolve(Arg.Any<string>()).Returns(call =>
                CalculatedWageCodes.Contains(call.Arg<string>())
                    ? Substitute.For<IWageTypeCalculator>()
                    : null);

            calculatorResolver.TryCalculateAsync(Arg.Any<PayrollCalculationContext>()).Returns(call =>
            {
                var context = call.Arg<PayrollCalculationContext>();

                if (ThrowForEmployeeId != null && context.EmployeeId == ThrowForEmployeeId.Value)
                {
                    throw new InvalidOperationException("boom");
                }

                DispatchedWageCodes.Add(context.WageType.Code);
                return Task.FromResult(true);
            });

            return new PayrollEngineService(
                Writable(Employees),
                Writable(OrgAssignments),
                Writable(Contracts),
                Writable(PayrollStatuses),
                Writable(OnHolds),
                Writable(Absences),
                Writable(AbsAdjustments),
                Writable(FnFPayments),
                Writable(Separations),
                Writable(Payments),
                Writable(RecurringPayments),
                Writable(Overtimes),
                Writable(Attendances),
                Writable(SubGroupPayments),
                Writable(SalaryRegisters),
                Writable(new List<PayrollControlRecord> { ControlRecord }),
                Writable(PayrollCalendars),
                Writable(new List<PayrollArea> { PayrollArea }),
                Writable(ReasonTypes),
                Writable(PayrollLogs),
                Writable(WageTypes),
                Writable(WageTypeExceptions),
                Writable(TimeRegisters),
                Writable(ExecutionLogs),
                settingsAccessor,
                areaResolver,
                calculatorResolver,
                BasicPayService,
                LeavePayService,
                new FakePayrollRegisterStore());
        }
    }

    /// <summary>
    /// A WRITABLE in-memory <see cref="IRepository{TEntity}"/>. <c>PayrollEngineFakes.Repo</c> is read-only
    /// (by design -- the wage-rule waves never insert), but the engine creates
    /// <see cref="SalaryRegister"/>/<see cref="PayrollLog"/>/<see cref="ExecutionLog"/> rows and must read
    /// identities back, so <c>InsertAndGetIdAsync</c> has to work here.
    /// </summary>
    private static IRepository<TEntity> Writable<TEntity>(List<TEntity> list)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => list.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => list.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = NextId(list);
            }

            list.Add(entity);
            return Task.FromResult(entity);
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = NextId(list);
            }

            list.Add(entity);
            return Task.FromResult(entity.Id);
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call => Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            list.Remove(call.Arg<TEntity>());
            return Task.CompletedTask;
        });

        repository.FirstOrDefaultAsync(Arg.Any<int>()).Returns(call =>
        {
            var id = call.Arg<int>();
            return Task.FromResult(list.FirstOrDefault(e => e.Id == id));
        });

        return repository;
    }

    private static int NextId<TEntity>(List<TEntity> list) where TEntity : IEntity<int>
    {
        return list.Count == 0 ? 1 : list.Max(e => e.Id) + 1;
    }

    #endregion
}
