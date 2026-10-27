using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- <see cref="PayrollRegisterStore"/>, the single concrete-entity boundary between the ~34
/// wage-type rules of waves P4-P6 and the register tables.
///
/// <para><b>The most important assertions in this file are the Simulation ones.</b> Wave P7 gave
/// <see cref="PayrollRegisterTarget.Simulation"/> its own branch over the simulation tables. Every test there
/// also proves the live line and day tables saw nothing but reads, and that without the simulation tables the
/// store refuses instead of falling back to the live ones: a simulation run that silently wrote into
/// <c>SalaryRegisterDetail</c> would corrupt real payroll results with no visible symptom.</para>
///
/// <para>Read paths are covered against in-memory repositories. Write paths that go through
/// <c>InsertOrUpdateAndGetIdAsync</c>/<c>DeleteAsync</c> are exercised only for their target guard here; their
/// persistence behaviour belongs to the app-service-level tests that own the real DbContext.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRegisterStore_Tests
{
    private const int RegisterId = 100;

    #region << Builders >>

    private static PayrollRegisterRef Ref(PayrollRegisterTarget target = PayrollRegisterTarget.Live, int registerId = RegisterId)
    {
        return new PayrollRegisterRef
        {
            RegisterId = registerId,
            Target = target,
            ForPeriod = new DateTime(2025, 3, 31),
            InPeriod = new DateTime(2025, 3, 31),
            PayrollRunType = PayrollRunType.Regular
        };
    }

    private static WageType Wage(int id, string code)
    {
        return new WageType
        {
            Id = id,
            TenantId = 1,
            Code = code,
            NameEn = "Wage " + code,
            NameAr = "Wage " + code,
            BillingType = BillingType.Earning,
            PayrollOrder = id,
            Status = true
        };
    }

    private static PayrollRegisterStore BuildStore(
        IEnumerable<SalaryRegister> registers = null,
        IEnumerable<SalaryRegisterDetail> details = null,
        IEnumerable<SalaryRegisterDaysDetail> dayDetails = null,
        IEnumerable<WageType> wageTypes = null)
    {
        return new PayrollRegisterStore(
            Repo(registers ?? Enumerable.Empty<SalaryRegister>()),
            Repo(details ?? Enumerable.Empty<SalaryRegisterDetail>()),
            Repo(dayDetails ?? Enumerable.Empty<SalaryRegisterDaysDetail>()),
            Repo(wageTypes ?? Enumerable.Empty<WageType>()));
    }

    private static PayrollRegisterStore BuildPopulatedStore()
    {
        var basicPay = Wage(1, "1000");
        var netPay = Wage(2, "9000");

        return BuildStore(
            registers: new[]
            {
                new SalaryRegister
                {
                    Id = RegisterId, TenantId = 1, EmployeeId = 7,
                    ForPeriod = new DateTime(2025, 3, 31), InPeriod = new DateTime(2025, 3, 31)
                }
            },
            details: new[]
            {
                new SalaryRegisterDetail { Id = 1, TenantId = 1, SalaryRegisterId = RegisterId, WageTypeId = basicPay.Id, Rate = 1, Number = 30, Amount = 300 },
                new SalaryRegisterDetail { Id = 2, TenantId = 1, SalaryRegisterId = RegisterId, WageTypeId = netPay.Id, Rate = 0, Number = 0, Amount = 250 },
                new SalaryRegisterDetail { Id = 3, TenantId = 1, SalaryRegisterId = RegisterId + 1, WageTypeId = basicPay.Id, Amount = 999 }
            },
            wageTypes: new[] { basicPay, netPay });
    }

    #endregion

    #region << Simulation target (wave P7): the simulation tables, never the live ones >>

    /// <summary>A simulation register 500 with a 1000 line, beside the live register of the same employee.</summary>
    private sealed class SimulationFixture
    {
        public const int SimulationRegisterId = 500;

        public readonly WageType BasicPay = Wage(1, "1000");
        public readonly WageType NetPay = Wage(2, "9000");
        public readonly SimulationRegisterTestRepositories Simulation = new SimulationRegisterTestRepositories();
        public readonly SalaryRegisterDetail LiveLine;
        public readonly IRepository<SalaryRegisterDetail> LiveDetails;
        public readonly IRepository<SalaryRegisterDaysDetail> LiveDays;

        public SimulationFixture()
        {
            LiveLine = new SalaryRegisterDetail { Id = 1, TenantId = 1, SalaryRegisterId = RegisterId, WageTypeId = BasicPay.Id, Rate = 1, Number = 30, Amount = 300 };
            LiveDetails = Repo(LiveLine);
            LiveDays = Repo<SalaryRegisterDaysDetail>();

            Simulation.Headers.Add(new SimulationSalaryRegister
            {
                Id = SimulationRegisterId, TenantId = 1, EmployeeId = 7,
                ForPeriod = new DateTime(2025, 3, 31), InPeriod = new DateTime(2025, 3, 31)
            });
            Simulation.Details.Add(new SimulationSalaryRegisterDetail
            {
                Id = 41, TenantId = 1, SimulationSalaryRegisterId = SimulationRegisterId, WageTypeId = BasicPay.Id, Rate = 2, Number = 30, Amount = 600
            });
            Simulation.DayDetails.Add(new SimulationSalaryRegisterDaysDetail
            {
                Id = 71, TenantId = 1, SimulationSalaryRegisterDetailId = 41, Period = new DateTime(2025, 3, 31), Amount = 600
            });
        }

        public PayrollRegisterStore Build(bool withSimulationRows = true)
        {
            var store = new PayrollRegisterStore(
                Repo(new SalaryRegister { Id = RegisterId, TenantId = 1, EmployeeId = 7 }),
                LiveDetails,
                LiveDays,
                Repo(BasicPay, NetPay));

            // Built by hand, so the unit-of-work manager ABP would inject is set here (its Current saves nothing).
            store.UnitOfWorkManager = Substitute.For<IUnitOfWorkManager>();

            if (withSimulationRows)
            {
                store.SimulationRows = Simulation.BuildRows();
            }

            return store;
        }

        /// <summary>Nothing but reads ever reached the live line and day tables.</summary>
        public void LiveShouldBeUntouched()
        {
            LiveLine.Amount.ShouldBe(300, "a simulation run must never change a live SalaryRegisterDetail");
            LiveDetails.ReceivedCalls().ShouldAllBe(c => c.GetMethodInfo().Name == "GetAll");
            LiveDays.ReceivedCalls().ShouldAllBe(c => c.GetMethodInfo().Name == "GetAll");
        }
    }

    private static PayrollRegisterRef SimulationRef() =>
        Ref(PayrollRegisterTarget.Simulation, SimulationFixture.SimulationRegisterId);

    [Fact]
    public async Task GetLines_Should_Read_The_Simulation_Register_For_The_Simulation_Target()
    {
        var fixture = new SimulationFixture();

        var lines = await fixture.Build().GetLinesAsync(SimulationRef());

        var line = lines.ShouldHaveSingleItem();
        line.Id.ShouldBe(41);
        line.WageCode.ShouldBe("1000");
        line.Amount.ShouldBe(600);
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task GetLine_And_GetLineByWageTypeId_Should_Read_The_Simulation_Register()
    {
        var store = new SimulationFixture().Build();

        (await store.GetLineAsync(SimulationRef(), "1000")).Amount.ShouldBe(600);
        (await store.GetLineByWageTypeIdAsync(SimulationRef(), 1)).Amount.ShouldBe(600);
        (await store.GetLineAsync(SimulationRef(), "9000")).ShouldBeNull();
    }

    [Fact]
    public async Task SaveLine_Should_Update_The_Simulation_Line_And_Leave_The_Live_Line_Alone()
    {
        var fixture = new SimulationFixture();

        var saved = await fixture.Build().SaveLineAsync(SimulationRef(), new PayrollRegisterLine { WageCode = "1000", Rate = 3, Number = 30, Amount = 900 });

        saved.Id.ShouldBe(41);
        var simulationLine = fixture.Simulation.Details.ShouldHaveSingleItem();
        simulationLine.Rate.ShouldBe(3);
        simulationLine.Amount.ShouldBe(900);
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task SaveLine_Should_Add_A_Simulation_Line_Carrying_The_Simulation_Registers_Tenant()
    {
        var fixture = new SimulationFixture();

        await fixture.Build().SaveLineAsync(SimulationRef(), new PayrollRegisterLine { WageCode = "9000", Amount = 550 }, deleteWhenAmountIsZero: false);

        var added = fixture.Simulation.Details.Single(d => d.WageTypeId == fixture.NetPay.Id);
        added.SimulationSalaryRegisterId.ShouldBe(SimulationFixture.SimulationRegisterId);
        added.TenantId.ShouldBe(1);
        added.Amount.ShouldBe(550);
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task SaveLine_Should_Delete_A_Zero_Simulation_Line_With_Its_Days_As_Legacy()
    {
        var fixture = new SimulationFixture();

        var result = await fixture.Build().SaveLineAsync(SimulationRef(), new PayrollRegisterLine { WageCode = "1000", Amount = 0 });

        result.ShouldBeNull();
        fixture.Simulation.Details.ShouldBeEmpty();
        fixture.Simulation.DayDetails.ShouldBeEmpty();
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task ClearLines_Should_Empty_Only_The_Simulation_Register()
    {
        var fixture = new SimulationFixture();

        await fixture.Build().ClearLinesAsync(SimulationRef());

        fixture.Simulation.Details.ShouldBeEmpty();
        fixture.Simulation.DayDetails.ShouldBeEmpty();
        fixture.Simulation.Headers.ShouldHaveSingleItem("the register itself stays; only its lines go");
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task DeleteLine_Should_Delete_Only_The_Simulation_Line()
    {
        var fixture = new SimulationFixture();

        await fixture.Build().DeleteLineAsync(SimulationRef(), 1);

        fixture.Simulation.Details.ShouldBeEmpty();
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task SaveDayDetails_Should_Replace_The_Simulation_Lines_Days()
    {
        var fixture = new SimulationFixture();

        await fixture.Build().SaveDayDetailsAsync(SimulationRef(), 41, new List<SalaryRegisterDayCount>
        {
            new SalaryRegisterDayCount { Period = new DateTime(2025, 2, 28), Rate = 1, Number = 2, Amount = 3 },
            new SalaryRegisterDayCount { Period = new DateTime(2025, 3, 31), Rate = 4, Number = 5, Amount = 6 }
        });

        fixture.Simulation.DayDetails.Count.ShouldBe(2);
        fixture.Simulation.DayDetails.ShouldAllBe(d => d.SimulationSalaryRegisterDetailId == 41 && d.TenantId == 1);
        fixture.Simulation.DayDetails.ShouldNotContain(d => d.Id == 71, "the old day row is replaced");
        fixture.LiveShouldBeUntouched();
    }

    /// <summary>
    /// The one rule that must never be "simplified" away: without the simulation tables the store refuses -- it
    /// does NOT fall back to the live registers.
    /// </summary>
    [Fact]
    public async Task A_Simulation_Write_Should_Be_Refused_Rather_Than_Fall_Back_To_The_Live_Register()
    {
        var fixture = new SimulationFixture();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => fixture.Build(withSimulationRows: false)
            .SaveLineAsync(SimulationRef(), new PayrollRegisterLine { WageCode = "1000", Amount = 12345 }));

        ex.Message.ShouldContain("simulation");
        fixture.LiveShouldBeUntouched();
    }

    [Fact]
    public async Task Every_Entry_Point_Should_Reject_A_Null_Register()
    {
        var store = BuildPopulatedStore();

        await Should.ThrowAsync<ArgumentNullException>(() => store.GetLinesAsync(null));
        await Should.ThrowAsync<ArgumentNullException>(() => store.GetLineAsync(null, "1000"));
        await Should.ThrowAsync<ArgumentNullException>(() => store.GetLineByWageTypeIdAsync(null, 1));
        await Should.ThrowAsync<ArgumentNullException>(() => store.ClearLinesAsync(null));
        await Should.ThrowAsync<ArgumentNullException>(() => store.DeleteLineAsync(null, 1));
    }

    #endregion

    #region << Live reads >>

    [Fact]
    public async Task GetLines_Should_Return_Only_The_Requested_Registers_Lines_With_Their_Wage_Codes()
    {
        var lines = await BuildPopulatedStore().GetLinesAsync(Ref());

        lines.Count.ShouldBe(2);
        lines.Select(l => l.WageCode).ShouldBe(new[] { "1000", "9000" }, ignoreOrder: true);

        var basic = lines.Single(l => l.WageCode == "1000");
        basic.Id.ShouldBe(1);
        basic.WageTypeId.ShouldBe(1);
        basic.Rate.ShouldBe(1);
        basic.Number.ShouldBe(30);
        basic.Amount.ShouldBe(300);
    }

    [Fact]
    public async Task GetLine_Should_Find_By_Wage_Code_And_Return_Null_When_Absent()
    {
        var store = BuildPopulatedStore();

        (await store.GetLineAsync(Ref(), "9000")).Amount.ShouldBe(250);
        (await store.GetLineAsync(Ref(), "4711")).ShouldBeNull();
    }

    [Fact]
    public async Task GetLineByWageTypeId_Should_Find_By_Id_And_Return_Null_When_Absent()
    {
        var store = BuildPopulatedStore();

        (await store.GetLineByWageTypeIdAsync(Ref(), 2)).WageCode.ShouldBe("9000");
        (await store.GetLineByWageTypeIdAsync(Ref(), 99)).ShouldBeNull();
    }

    #endregion

    #region << Live writes: argument contract >>

    [Fact]
    public async Task SaveLine_Should_Reject_A_Null_Line()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => BuildPopulatedStore().SaveLineAsync(Ref(), null));
    }

    [Fact]
    public async Task SaveLine_Should_Reject_A_Line_With_Neither_WageTypeId_Nor_WageCode()
    {
        await Should.ThrowAsync<ArgumentException>(() => BuildPopulatedStore().SaveLineAsync(
            Ref(), new PayrollRegisterLine { Amount = 1 }, deleteWhenAmountIsZero: false));
    }

    [Fact]
    public async Task SaveLine_Should_Throw_When_The_Wage_Code_Does_Not_Exist()
    {
        // Legacy Program.CalculateTimeType (line 2728): "WageType {code} : ... not found".
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => BuildPopulatedStore().SaveLineAsync(
            Ref(), new PayrollRegisterLine { WageCode = "4711", Amount = 1 }, deleteWhenAmountIsZero: false));

        ex.Message.ShouldContain("4711");
    }

    [Fact]
    public async Task SaveLine_Should_Throw_When_The_Register_Row_Is_Missing()
    {
        var basicPay = Wage(1, "1000");
        var store = BuildStore(wageTypes: new[] { basicPay });

        await Should.ThrowAsync<InvalidOperationException>(() => store.SaveLineAsync(
            Ref(), new PayrollRegisterLine { WageCode = "1000", Amount = 1 }, deleteWhenAmountIsZero: false));
    }

    [Fact]
    public async Task SaveLine_Should_Return_Null_And_Create_Nothing_When_Amount_Is_Zero()
    {
        // Legacy "zero means delete" (EmployeeCalculatedPayService.cs lines 211-216 etc.): a zero-amount
        // calculation must leave NO row behind, not a zero row.
        var basicPay = Wage(1, "1000");
        var store = BuildStore(
            registers: new[] { new SalaryRegister { Id = RegisterId, TenantId = 1, EmployeeId = 7 } },
            wageTypes: new[] { basicPay });

        var result = await store.SaveLineAsync(Ref(), new PayrollRegisterLine { WageCode = "1000", Amount = 0 });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveDayDetails_Should_Throw_When_The_Line_Does_Not_Exist()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => BuildPopulatedStore().SaveDayDetailsAsync(
            Ref(), 4711, new List<SalaryRegisterDayCount>()));
    }

    #endregion
}
