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
/// Wave P5 -- the seven provision-cluster <see cref="IWageTypeCalculator"/> rules
/// (1010, 3000, 3003, 3001, 3004, 3002, 3005) ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 174-393.
///
/// <para>These rules are branch SCAFFOLDING -- run-type gate, find-or-create line, delegate, zero-means-delete
/// -- so that is what is asserted here: the gates, the delegation targets, legacy's ORDER of operations, and
/// the zero-means-delete. The money maths they delegate to is pinned by
/// <c>IndemnityCalculationServiceTests</c> (wave P3) and <c>AnnualLeaveProvisionServiceTests</c> (this wave).
/// The 3002/3005 rules DO carry their own maths and it is covered here.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProvisionWageTypeCalculators_Tests
{
    private const int RegisterId = 500;


    #region << Builders >>

    private static WageType Wage(int id, string code, bool includeInAdvancePay = false, double fixedDays = 26)
    {
        return new WageType
        {
            Id = id,
            TenantId = 1,
            Code = code,
            NameEn = "Wage " + code,
            BillingType = BillingType.Provision,
            IncludeInAdvancePay = includeInAdvancePay,
            FixedDays = fixedDays,
            Status = true
        };
    }

    private static PayrollCalculationContext Context(
        WageType wageType,
        PayrollRunType runType = PayrollRunType.Regular,
        string offCycleReason = null)
    {
        var payDate = new DateTime(2025, 3, 31);

        return new PayrollCalculationContext
        {
            EmployeeId = 7,
            PayDate = payDate,
            PayStartDate = new DateTime(2025, 3, 1),
            PayInDate = payDate,
            PayrollRunType = runType,
            OffCycleReason = offCycleReason,
            WageType = wageType,
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = payDate,
                InPeriod = payDate,
                PayrollRunType = runType
            }
        };
    }

    #endregion

    #region << 1010 Indemnity Pay (legacy 174-185) >>

    [Fact]
    public async Task IndemnityPay_Should_Delegate_To_IndemnityCalculationService_On_A_Regular_Run()
    {
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetIndemnityPayAsync(default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityPayResult()));

        var wageType = Wage(10, "1010");
        var context = Context(wageType);

        await new IndemnityPayWageTypeCalculator(service).CalculateAsync(context);

        await service.Received(1).GetIndemnityPayAsync(
            7, RegisterId, 10, context.PayDate, PayrollRunType.Regular, null, null, null);
    }

    /// <summary>Legacy line 177: the 1010 branch gates on the SPECIFIC indemnity hold, not the generic one.</summary>
    [Fact]
    public async Task IndemnityPay_Should_Do_Nothing_When_IndemnityPay_Is_On_Hold()
    {
        var service = Substitute.For<IIndemnityCalculationService>();

        var context = Context(Wage(10, "1010"));
        context.OnHold = new PayrollOnHoldFlags { IndemnityPay = true };

        await new IndemnityPayWageTypeCalculator(service).CalculateAsync(context);

        await service.DidNotReceiveWithAnyArgs().GetIndemnityPayAsync(default, default, default, default, default, default, default, default);
    }

    /// <summary>Legacy line 179: off-cycle only for End Of Service.</summary>
    [Fact]
    public async Task IndemnityPay_Should_Do_Nothing_On_An_OffCycle_Run_That_Is_Not_End_Of_Service()
    {
        var service = Substitute.For<IIndemnityCalculationService>();

        var context = Context(Wage(10, "1010"), PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave);

        await new IndemnityPayWageTypeCalculator(service).CalculateAsync(context);

        await service.DidNotReceiveWithAnyArgs().GetIndemnityPayAsync(default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task IndemnityPay_Should_Run_On_An_End_Of_Service_OffCycle_Run()
    {
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetIndemnityPayAsync(default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityPayResult()));

        var context = Context(Wage(10, "1010"), PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService);

        await new IndemnityPayWageTypeCalculator(service).CalculateAsync(context);

        await service.ReceivedWithAnyArgs(1).GetIndemnityPayAsync(default, default, default, default, default, default, default, default);
    }

    #endregion

    #region << 3000 Indemnity Provision (legacy 187-219) >>

    [Fact]
    public async Task IndemnityProvision_Should_Create_The_Line_Then_Post_The_Calculated_Amount()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Rate = 1500, Number = 2.5, Amount = 144.23 }));

        var wageType = Wage(30, "3000");
        var context = Context(wageType);

        await new IndemnityProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        var line = store.Lines.Single();
        line.WageTypeId.ShouldBe(30);
        line.Rate.ShouldBe(1500);
        line.Number.ShouldBe(2.5);
        line.Amount.ShouldBe(144.23);

        // The line must have existed (with a real Id) BEFORE the service call -- the wave-P3 API takes a
        // salaryRegisterDetailId.
        await service.Received(1).GetIndemnityProvisionAsync(
            7, line.Id, context.PayDate, PayrollRunType.Regular, null, null, null);
    }

    /// <summary>Legacy lines 211-216.</summary>
    [Fact]
    public async Task IndemnityProvision_Should_Delete_The_Line_When_The_Amount_Is_Zero()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Rate = 1500, Number = 2.5, Amount = 0 }));

        await new IndemnityProvisionWageTypeCalculator(service, store)
            .CalculateAsync(Context(Wage(30, "3000")));

        store.Lines.ShouldBeEmpty();
        store.DeletedLineIds.Count.ShouldBe(1);
    }

    /// <summary>Legacy lines 190-192: off-cycle needs BOTH Advance Leave AND IncludeInAdvancePay.</summary>
    [Theory]
    [InlineData(PayrollOffCycleReasons.AdvanceLeave, true, true)]
    [InlineData(PayrollOffCycleReasons.AdvanceLeave, false, false)]
    [InlineData(PayrollOffCycleReasons.EndOfService, true, false)]
    [InlineData(PayrollOffCycleReasons.LeaveEncashment, true, false)]
    public async Task IndemnityProvision_Should_Apply_The_Legacy_Advance_Leave_Gate(string reason, bool includeInAdvancePay, bool shouldRun)
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Amount = 10 }));

        var context = Context(Wage(30, "3000", includeInAdvancePay), PayrollRunType.OffCycle, reason);

        await new IndemnityProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        if (shouldRun)
        {
            await service.ReceivedWithAnyArgs(1).GetIndemnityProvisionAsync(default, default, default, default, default, default, default);
        }
        else
        {
            await service.DidNotReceiveWithAnyArgs().GetIndemnityProvisionAsync(default, default, default, default, default, default, default);
            store.Lines.ShouldBeEmpty();
        }
    }

    #endregion

    #region << 3003 Total Indemnity Provision (legacy 221-257) >>

    [Fact]
    public async Task TotalIndemnityProvision_Should_Post_The_Amount_And_Skip_The_DayWise_Tail_By_Default()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetTotalIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Rate = 1500, Number = 40, Amount = 2307.69 }));

        await new TotalIndemnityProvisionWageTypeCalculator(service, store)
            .CalculateAsync(Context(Wage(33, "3003")));

        store.Lines.Single().Amount.ShouldBe(2307.69);

        await service.DidNotReceiveWithAnyArgs().GetIndemnityProvisionDayWiseEntryAsync(default, default, default);
    }

    /// <summary>
    /// Legacy lines 252-254 -- and the ORDER matters: the 3003 zero-means-delete (245-250) must already have
    /// happened when the day-wise pass runs, because that pass touches the 3000 line and may re-create it.
    /// </summary>
    [Fact]
    public async Task TotalIndemnityProvision_Should_Run_The_DayWise_Tail_After_Saving_When_The_Settings_Allow()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetTotalIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Amount = 0 }));

        var lineCountWhenTailRan = -1;
        service.GetIndemnityProvisionDayWiseEntryAsync(default, default, default)
            .ReturnsForAnyArgs(_ =>
            {
                lineCountWhenTailRan = store.Lines.Count;
                return Task.FromResult(new IndemnityProvisionDayWiseResult());
            });

        var context = Context(Wage(33, "3003"));
        context.Settings.IncludeSalaryIncProvDiffInSalReg = true;
        context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt = new DateTime(2020, 1, 1);

        await new TotalIndemnityProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        await service.Received(1).GetIndemnityProvisionDayWiseEntryAsync(7, RegisterId, context.PayDate);

        // Amount was 0 => the 3003 line was already deleted before the tail ran.
        lineCountWhenTailRan.ShouldBe(0);
    }

    /// <summary>Legacy line 253 compares STRICTLY less-than against payDate.</summary>
    [Fact]
    public async Task TotalIndemnityProvision_Should_Skip_The_DayWise_Tail_When_The_Effective_Date_Is_Not_Before_PayDate()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IIndemnityCalculationService>();
        service.GetTotalIndemnityProvisionAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(new IndemnityProvisionResult { Amount = 5 }));

        var context = Context(Wage(33, "3003"));
        context.Settings.IncludeSalaryIncProvDiffInSalReg = true;
        context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt = context.PayDate;

        await new TotalIndemnityProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        await service.DidNotReceiveWithAnyArgs().GetIndemnityProvisionDayWiseEntryAsync(default, default, default);
    }

    #endregion

    #region << 3001 / 3004 Annual Leave Provision (legacy 259-329) >>

    [Fact]
    public async Task AnnualLeaveProvision_Should_Post_The_Amount_And_Then_Run_The_DayWise_Tail()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IAnnualLeaveProvisionService>();
        service.GetAnnualLeaveProvisionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new AnnualLeaveProvisionAmounts { Rate = 1300, Number = 2.5, Amount = 125 }));
        service.GetAnnualLeaveProvisionDayWiseEntryAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new AnnualLeaveProvisionDayWiseResult()));

        var context = Context(Wage(31, "3001"));
        context.Settings.IncludeSalaryIncProvDiffInSalReg = true;
        context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt = new DateTime(2020, 1, 1);

        await new AnnualLeaveProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        store.Lines.Single().Amount.ShouldBe(125);
        await service.Received(1).GetAnnualLeaveProvisionDayWiseEntryAsync(context);
    }

    [Fact]
    public async Task AnnualLeaveProvision_Should_Delete_The_Line_When_The_Amount_Is_Zero()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IAnnualLeaveProvisionService>();
        service.GetAnnualLeaveProvisionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new AnnualLeaveProvisionAmounts { Rate = 1300, Number = 0, Amount = 0 }));

        await new AnnualLeaveProvisionWageTypeCalculator(service, store)
            .CalculateAsync(Context(Wage(31, "3001")));

        store.Lines.ShouldBeEmpty();
    }

    /// <summary>Legacy attaches the day-wise tail to 3001 but NOT to 3004 (compare lines 290-292 with 320-327).</summary>
    [Fact]
    public async Task TotalAnnualLeaveProvision_Should_Have_No_DayWise_Tail()
    {
        var store = new FakePayrollRegisterStore();
        var service = Substitute.For<IAnnualLeaveProvisionService>();
        service.GetTotalAnnualLeaveProvisionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new AnnualLeaveProvisionAmounts { Rate = 1300, Number = 40, Amount = 2000 }));

        var context = Context(Wage(34, "3004"));
        context.Settings.IncludeSalaryIncProvDiffInSalReg = true;
        context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt = new DateTime(2020, 1, 1);

        await new TotalAnnualLeaveProvisionWageTypeCalculator(service, store).CalculateAsync(context);

        store.Lines.Single().Amount.ShouldBe(2000);
        await service.DidNotReceiveWithAnyArgs().GetAnnualLeaveProvisionDayWiseEntryAsync(default);
    }

    #endregion

    #region << 3002 Ticket Provision (legacy 331-361 / 2922-2978) >>

    private static TicketProvisionWageTypeCalculator TicketCalculator(
        IPayrollRegisterStore store,
        IEnumerable<EmployeeTicketRoute> routes = null,
        IEnumerable<TicketPaxType> paxTypes = null,
        IEnumerable<TicketFare> fares = null)
    {
        return new TicketProvisionWageTypeCalculator(
            store,
            Repo(routes ?? new List<EmployeeTicketRoute>()),
            Repo(paxTypes ?? new List<TicketPaxType>()),
            Repo(fares ?? new List<TicketFare>()));
    }

    private static HCMv3.TimeManagement.TimeRegister Day(DateTime date, double ticketAccrual)
    {
        return new HCMv3.TimeManagement.TimeRegister { EmployeeId = 7, DayDate = date, TicketAccrualPerDay = ticketAccrual };
    }

    [Fact]
    public async Task TicketProvision_Should_Post_Accrued_Tickets_Times_The_Adult_Economy_Return_Fare()
    {
        var store = new FakePayrollRegisterStore();

        var context = Context(Wage(32, "3002"));
        context.TimeRegisterList = new List<HCMv3.TimeManagement.TimeRegister>
        {
            Day(new DateTime(2025, 3, 10), 0.05),
            Day(new DateTime(2025, 3, 11), 0.05),
            Day(new DateTime(2025, 2, 28), 99) // outside the period -- must be ignored
        };

        var routes = new[] { new EmployeeTicketRoute { Id = 1, EmployeeId = 7, TicketRouteId = 4, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2030, 1, 1) } };
        var paxTypes = new[] { new TicketPaxType { Id = 2, Code = "A", NameEn = "Adult" } };
        var fares = new[]
        {
            new TicketFare
            {
                Id = 3, TicketRouteId = 4, TicketPaxTypeId = 2, TicketClassId = 9,
                TicketClass = new TicketClass { Id = 9, Code = "2", NameEn = "Economy" },
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2030, 1, 1),
                ReturnTicketFare = 300
            }
        };

        await TicketCalculator(store, routes, paxTypes, fares).CalculateAsync(context);

        var line = store.Lines.Single();
        line.Number.ShouldBe(0.1, 1e-9);
        line.Rate.ShouldBe(300);
        line.Amount.ShouldBe(30, 1e-9);
    }

    /// <summary>
    /// Legacy line 2945: with no accrued ticket days legacy writes NOTHING, so a pre-existing line keeps its
    /// OLD values and survives the zero-means-delete on those old values.
    /// </summary>
    [Fact]
    public async Task TicketProvision_Should_Leave_An_Existing_Line_Untouched_When_No_Tickets_Accrued()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 77, WageTypeId = 32, WageCode = "3002", Rate = 250, Number = 0.4, Amount = 100 });

        var context = Context(Wage(32, "3002"));
        context.TimeRegisterList = new List<HCMv3.TimeManagement.TimeRegister>();

        await TicketCalculator(store).CalculateAsync(context);

        var line = store.Lines.Single();
        line.Rate.ShouldBe(250);
        line.Number.ShouldBe(0.4);
        line.Amount.ShouldBe(100);
    }

    /// <summary>Legacy lines 2966-2971: the "no ticket route" path is the ONLY one that explicitly zeroes.</summary>
    [Fact]
    public async Task TicketProvision_Should_Zero_And_Delete_The_Line_When_The_Employee_Has_No_Ticket_Route()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 77, WageTypeId = 32, WageCode = "3002", Rate = 250, Number = 0.4, Amount = 100 });

        var context = Context(Wage(32, "3002"));
        context.TimeRegisterList = new List<HCMv3.TimeManagement.TimeRegister> { Day(new DateTime(2025, 3, 10), 0.05) };

        await TicketCalculator(store).CalculateAsync(context);

        store.Lines.ShouldBeEmpty();
        store.DeletedLineIds.ShouldContain(77);
    }

    /// <summary>Legacy line 2957: no fare row => write nothing, line keeps its previous values.</summary>
    [Fact]
    public async Task TicketProvision_Should_Leave_An_Existing_Line_Untouched_When_No_Fare_Row_Matches()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 77, WageTypeId = 32, WageCode = "3002", Rate = 250, Number = 0.4, Amount = 100 });

        var context = Context(Wage(32, "3002"));
        context.TimeRegisterList = new List<HCMv3.TimeManagement.TimeRegister> { Day(new DateTime(2025, 3, 10), 0.05) };

        var routes = new[] { new EmployeeTicketRoute { Id = 1, EmployeeId = 7, TicketRouteId = 4, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2030, 1, 1) } };
        var paxTypes = new[] { new TicketPaxType { Id = 2, Code = "A", NameEn = "Adult" } };

        await TicketCalculator(store, routes, paxTypes).CalculateAsync(context);

        store.Lines.Single().Amount.ShouldBe(100);
    }

    /// <summary>
    /// Legacy lines 2938-2942: an off-cycle run reads the unfiltered TimeRegister QUERY, because legacy sets
    /// the preloaded LIST to null on off-cycle runs (Program.cs line 1771).
    /// </summary>
    [Fact]
    public async Task TicketProvision_Should_Read_The_TimeRegister_Query_On_An_OffCycle_Run()
    {
        var store = new FakePayrollRegisterStore();

        var context = Context(Wage(32, "3002"), PayrollRunType.OffCycle, PayrollOffCycleReasons.EndOfService);
        context.TimeRegisterList = null; // exactly what legacy does
        context.TimeRegisterQuery = new List<HCMv3.TimeManagement.TimeRegister>
        {
            Day(new DateTime(2025, 3, 10), 0.2),
            new HCMv3.TimeManagement.TimeRegister { EmployeeId = 99, DayDate = new DateTime(2025, 3, 10), TicketAccrualPerDay = 5 }
        }.AsQueryable();

        var routes = new[] { new EmployeeTicketRoute { Id = 1, EmployeeId = 7, TicketRouteId = 4, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2030, 1, 1) } };
        var paxTypes = new[] { new TicketPaxType { Id = 2, Code = "A", NameEn = "Adult" } };
        var fares = new[]
        {
            new TicketFare
            {
                Id = 3, TicketRouteId = 4, TicketPaxTypeId = 2, TicketClassId = 9,
                TicketClass = new TicketClass { Id = 9, Code = "2", NameEn = "Economy" },
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2030, 1, 1),
                ReturnTicketFare = 300
            }
        };

        await TicketCalculator(store, routes, paxTypes, fares).CalculateAsync(context);

        store.Lines.Single().Number.ShouldBe(0.2, 1e-9);
    }

    #endregion

    #region << 3005 Total Ticket Provision (legacy 363-393 / 2979-2998) >>

    [Fact]
    public async Task TotalTicketProvision_Should_Multiply_The_3002_Rate_By_The_4028_Plus_5028_Days()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 1, WageTypeId = 40, WageCode = "4028", Number = 1.5 });
        store.Lines.Add(new PayrollRegisterLine { Id = 2, WageTypeId = 50, WageCode = "5028", Number = 0.25 });
        store.Lines.Add(new PayrollRegisterLine { Id = 3, WageTypeId = 32, WageCode = "3002", Rate = 300, Number = 0.1, Amount = 30 });

        await new TotalTicketProvisionWageTypeCalculator(store).CalculateAsync(Context(Wage(35, "3005")));

        var line = store.Lines.Single(l => l.WageCode == "3005");
        line.Rate.ShouldBe(300);
        line.Number.ShouldBe(1.75);
        line.Amount.ShouldBe(525);
    }

    /// <summary>Each source lookup is a FirstOrDefault -- a missing line contributes 0, it is not an error.</summary>
    [Fact]
    public async Task TotalTicketProvision_Should_Delete_Itself_When_The_3002_Line_Is_Absent()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 1, WageTypeId = 40, WageCode = "4028", Number = 1.5 });

        await new TotalTicketProvisionWageTypeCalculator(store).CalculateAsync(Context(Wage(35, "3005")));

        store.Lines.Any(l => l.WageCode == "3005").ShouldBeFalse();
    }

    /// <summary>Legacy line 366: off-cycle only for End Of Service, and IncludeInAdvancePay is NOT consulted.</summary>
    [Fact]
    public async Task TotalTicketProvision_Should_Do_Nothing_On_An_Advance_Leave_OffCycle_Run()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 1, WageTypeId = 40, WageCode = "4028", Number = 1.5 });
        store.Lines.Add(new PayrollRegisterLine { Id = 3, WageTypeId = 32, WageCode = "3002", Rate = 300 });

        var context = Context(Wage(35, "3005", includeInAdvancePay: true), PayrollRunType.OffCycle, PayrollOffCycleReasons.AdvanceLeave);

        await new TotalTicketProvisionWageTypeCalculator(store).CalculateAsync(context);

        store.Lines.Any(l => l.WageCode == "3005").ShouldBeFalse();
    }

    #endregion
}
