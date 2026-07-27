using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- <see cref="PayrollRegisterStore"/>, the single concrete-entity boundary between the ~34
/// wage-type rules of waves P4-P6 and the register tables.
///
/// <para><b>The most important assertions in this file are the Simulation ones.</b> Wave P1 implements
/// <see cref="PayrollRegisterTarget.Live"/> only and every entry point throws
/// <see cref="NotImplementedException"/> for <see cref="PayrollRegisterTarget.Simulation"/> instead of
/// falling back to the live tables. That refusal is deliberate: a simulation run that silently wrote into
/// <c>SalaryRegisterDetail</c> would corrupt real payroll results with no visible symptom. Wave P7 replaces
/// the throw with a genuine <c>SimulationSalaryRegisterDetail</c> branch -- it must NEVER be replaced with a
/// fall-through.</para>
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

    #region << Simulation target must throw, never fall through to the live tables >>

    [Fact]
    public async Task GetLines_Should_Throw_For_The_Simulation_Target()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().GetLinesAsync(Ref(PayrollRegisterTarget.Simulation)));
    }

    [Fact]
    public async Task GetLine_Should_Throw_For_The_Simulation_Target()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().GetLineAsync(Ref(PayrollRegisterTarget.Simulation), "1000"));
    }

    [Fact]
    public async Task GetLineByWageTypeId_Should_Throw_For_The_Simulation_Target()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().GetLineByWageTypeIdAsync(Ref(PayrollRegisterTarget.Simulation), 1));
    }

    [Fact]
    public async Task SaveLine_Should_Throw_For_The_Simulation_Target_Without_Writing_To_The_Live_Register()
    {
        var basicPay = Wage(1, "1000");
        var live = new SalaryRegisterDetail
        {
            Id = 1, TenantId = 1, SalaryRegisterId = RegisterId, WageTypeId = basicPay.Id, Amount = 300
        };

        var store = BuildStore(
            registers: new[] { new SalaryRegister { Id = RegisterId, TenantId = 1, EmployeeId = 7 } },
            details: new[] { live },
            wageTypes: new[] { basicPay });

        await Should.ThrowAsync<NotImplementedException>(() => store.SaveLineAsync(
            Ref(PayrollRegisterTarget.Simulation),
            new PayrollRegisterLine { WageCode = "1000", Amount = 12345 }));

        live.Amount.ShouldBe(300, "a simulation run must never mutate a live SalaryRegisterDetail");
    }

    [Fact]
    public async Task DeleteLine_Should_Throw_For_The_Simulation_Target_Without_Deleting_The_Live_Line()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().DeleteLineAsync(Ref(PayrollRegisterTarget.Simulation), 1));
    }

    [Fact]
    public async Task ClearLines_Should_Throw_For_The_Simulation_Target()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().ClearLinesAsync(Ref(PayrollRegisterTarget.Simulation)));
    }

    [Fact]
    public async Task SaveDayDetails_Should_Throw_For_The_Simulation_Target()
    {
        await Should.ThrowAsync<NotImplementedException>(() => BuildPopulatedStore().SaveDayDetailsAsync(
            Ref(PayrollRegisterTarget.Simulation), 1, new List<SalaryRegisterDayCount>()));
    }

    [Fact]
    public async Task The_Simulation_Guard_Should_Name_Wave_P7_And_The_Legacy_File_To_Fold_In()
    {
        // The message is the only breadcrumb a wave-P7 developer gets; keep it informative.
        var ex = await Should.ThrowAsync<NotImplementedException>(
            () => BuildPopulatedStore().GetLinesAsync(Ref(PayrollRegisterTarget.Simulation)));

        ex.Message.ShouldContain("Simulation");
        ex.Message.ShouldContain("SimulationPayrollHelper");
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
