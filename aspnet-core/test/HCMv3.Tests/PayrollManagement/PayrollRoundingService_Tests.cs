using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- <see cref="PayrollRoundingService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/Program.SetRoundoff</c> (lines 3002-3101) and <c>Program.GetDecimalRounding</c>
/// (lines 3102-3121).
///
/// <para><b>These tests assert LEGACY behaviour, including behaviour that looks wrong.</b> The digit counts
/// are financial controls; <see cref="Math.Round(double,int)"/>'s midpoint-to-even ("banker's") rounding is
/// what legacy produced and therefore what every historical payslip in the live system was computed with. Do
/// not "improve" the service and then relax a test here -- a one-digit change silently re-values every
/// payroll line.</para>
///
/// <para>The service uses only synchronous <c>System.Linq</c> over <c>IRepository&lt;T&gt;.GetAll()</c> (no EF
/// Core reference in HCMv3.Core), so in-memory repositories are an exact stand-in and no database is
/// needed. Writes mutate the very objects handed in, so assertions are made against those instances.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRoundingService_Tests
{
    private const int EmployeeId = 7;
    private const int RegisterId = 100;
    private const long OrganizationUnitId = 55;

    #region << Builders >>

    private static SalaryRegister Register(DateTime forPeriod)
    {
        return new SalaryRegister
        {
            Id = RegisterId,
            TenantId = 1,
            EmployeeId = EmployeeId,
            ForPeriod = forPeriod,
            InPeriod = forPeriod,
            PayrollRunType = PayrollRunType.Regular
        };
    }

    private static PayrollRegisterRef Ref(DateTime forPeriod, PayrollRegisterTarget target = PayrollRegisterTarget.Live)
    {
        return new PayrollRegisterRef
        {
            RegisterId = RegisterId,
            Target = target,
            ForPeriod = forPeriod,
            InPeriod = forPeriod,
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

    private static SalaryRegisterDetail Detail(int id, int wageTypeId, double value)
    {
        return new SalaryRegisterDetail
        {
            Id = id,
            TenantId = 1,
            SalaryRegisterId = RegisterId,
            WageTypeId = wageTypeId,
            Rate = value,
            Number = value,
            Amount = value
        };
    }

    private static PayrollRoundingService BuildService(
        IEnumerable<SalaryRegister> registers = null,
        IEnumerable<SalaryRegisterDetail> details = null,
        IEnumerable<SalaryRegisterDaysDetail> dayDetails = null,
        IEnumerable<WageType> wageTypes = null,
        IEnumerable<EmployeeOrgAssignment> orgAssignments = null,
        IEnumerable<OrgUnitProfile> orgUnitProfiles = null,
        IEnumerable<Currency> currencies = null)
    {
        return new PayrollRoundingService(
            Repo(registers ?? Enumerable.Empty<SalaryRegister>()),
            Repo(details ?? Enumerable.Empty<SalaryRegisterDetail>()),
            Repo(dayDetails ?? Enumerable.Empty<SalaryRegisterDaysDetail>()),
            Repo(wageTypes ?? Enumerable.Empty<WageType>()),
            Repo(orgAssignments ?? Enumerable.Empty<EmployeeOrgAssignment>()),
            Repo(orgUnitProfiles ?? Enumerable.Empty<OrgUnitProfile>()),
            Repo(currencies ?? Enumerable.Empty<Currency>()));
    }

    #endregion

    #region << The ten always-6-decimals accrual wage codes (legacy Program.cs line 3025) >>

    [Fact]
    public void AccrualWageCodes_Should_Be_Exactly_Legacys_Ten()
    {
        // Legacy: string[] ALaccrualWT = { "4021","5021","4010","5010","4030","5030","4022","5022","4031","5031" };
        PayrollRoundingService.AccrualWageCodes.ShouldBe(
            new[] { "4021", "5021", "4010", "5010", "4030", "5030", "4022", "5022", "4031", "5031" },
            ignoreOrder: true);
    }

    #endregion

    #region << Current mode: DecimalInSalaryRegister, no wage-code exceptions (legacy lines 3074-3091) >>

    [Fact]
    public async Task ApplyRounding_Should_Use_DecimalInSalaryRegister_When_Decimal6RoundingFrom_Is_Unset()
    {
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var detail = Detail(1, wageType.Id, 1.23456789);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(Ref(forPeriod), new PayrollEngineSettings { DecimalInSalaryRegister = 3 });

        detail.Rate.ShouldBe(1.235);
        detail.Number.ShouldBe(1.235);
        detail.Amount.ShouldBe(1.235);
    }

    [Fact]
    public async Task ApplyRounding_Should_Use_DecimalInSalaryRegister_When_The_Period_Is_On_Or_After_Decimal6RoundingFrom()
    {
        // Legacy line 3010: the OLD mode applies only when ForPeriod is STRICTLY BEFORE Decimal6RoundingFrom.
        var cutOver = new DateTime(2025, 1, 1);
        var forPeriod = cutOver; // equal -> current mode
        var wageType = Wage(1, "4021"); // an accrual code -- must NOT get its 6-decimal exception here
        var detail = Detail(1, wageType.Id, 1.23456789);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(
            Ref(forPeriod),
            new PayrollEngineSettings { Decimal6RoundingFrom = cutOver, DecimalInSalaryRegister = 2 });

        detail.Amount.ShouldBe(1.23);
    }

    [Fact]
    public async Task ApplyRounding_Should_Default_DecimalInSalaryRegister_To_Six()
    {
        // Legacy applies its zero guard at load time: (DecimalInSalaryRegister == 0) ? 6 : ... (line 2640),
        // which this port encodes as the property's default.
        new PayrollEngineSettings().DecimalInSalaryRegister.ShouldBe(6);

        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var detail = Detail(1, wageType.Id, 1.2345678912345);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(Ref(forPeriod), new PayrollEngineSettings());

        detail.Amount.ShouldBe(1.234568);
    }

    [Fact]
    public async Task ApplyRounding_Should_Preserve_Legacys_Midpoint_To_Even_Rounding()
    {
        // Math.Round(double, int) is banker's rounding: 0.125 -> 0.12, NOT 0.13. Legacy used exactly this,
        // so every historical payslip in the live system carries it. 0.125 and 0.375 are exactly
        // representable as doubles, so this is a true midpoint test and not a float-artefact test.
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var down = Detail(1, wageType.Id, 0.125);
        var up = Detail(2, wageType.Id, 0.375);
        up.WageTypeId = wageType.Id;

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { down, up },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(Ref(forPeriod), new PayrollEngineSettings { DecimalInSalaryRegister = 2 });

        down.Amount.ShouldBe(0.12);
        up.Amount.ShouldBe(0.38);
    }

    [Fact]
    public async Task ApplyRounding_Should_Also_Round_The_Day_Detail_Rows()
    {
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var detail = Detail(1, wageType.Id, 1.23456789);
        var dayRow = new SalaryRegisterDaysDetail
        {
            Id = 1,
            TenantId = 1,
            SalaryRegisterDetailId = detail.Id,
            Period = forPeriod,
            Rate = 9.87654321,
            Number = 9.87654321,
            Amount = 9.87654321
        };

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            dayDetails: new[] { dayRow },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(Ref(forPeriod), new PayrollEngineSettings { DecimalInSalaryRegister = 2 });

        dayRow.Rate.ShouldBe(9.88);
        dayRow.Number.ShouldBe(9.88);
        dayRow.Amount.ShouldBe(9.88);
    }

    [Fact]
    public async Task ApplyRounding_Should_Only_Touch_The_Requested_Register()
    {
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var mine = Detail(1, wageType.Id, 1.23456789);
        var somebodyElses = Detail(2, wageType.Id, 1.23456789);
        somebodyElses.SalaryRegisterId = RegisterId + 1;

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { mine, somebodyElses },
            wageTypes: new[] { wageType });

        await service.ApplyRoundingAsync(Ref(forPeriod), new PayrollEngineSettings { DecimalInSalaryRegister = 2 });

        mine.Amount.ShouldBe(1.23);
        somebodyElses.Amount.ShouldBe(1.23456789);
    }

    #endregion

    #region << Old mode: currency decimals + the ten accrual exceptions (legacy lines 3029-3066) >>

    [Fact]
    public async Task ApplyRounding_Should_Use_Currency_Decimals_Before_The_Decimal6RoundingFrom_Cut_Over()
    {
        var cutOver = new DateTime(2025, 1, 1);
        var forPeriod = new DateTime(2024, 12, 31); // strictly before -> old mode
        var wageType = Wage(1, "1000");
        var detail = Detail(1, wageType.Id, 1.23456789);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            wageTypes: new[] { wageType },
            currencies: new[] { new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar", DecimalCount = 3 } });

        await service.ApplyRoundingAsync(
            Ref(forPeriod),
            new PayrollEngineSettings { Decimal6RoundingFrom = cutOver, DecimalInSalaryRegister = 2 });

        // 3 currency decimals win over DecimalInSalaryRegister = 2 in old mode.
        detail.Amount.ShouldBe(1.235);
    }

    [Theory]
    [InlineData("4021")]
    [InlineData("5021")]
    [InlineData("4010")]
    [InlineData("5010")]
    [InlineData("4030")]
    [InlineData("5030")]
    [InlineData("4022")]
    [InlineData("5022")]
    [InlineData("4031")]
    [InlineData("5031")]
    public async Task ApplyRounding_Should_Keep_Six_Decimals_For_The_Accrual_Wage_Codes_In_Old_Mode(string wageCode)
    {
        // Rounding an accrual RATE to the currency's 3 decimals loses material precision, which is why
        // legacy carved these ten codes out (line 3025).
        var cutOver = new DateTime(2025, 1, 1);
        var forPeriod = new DateTime(2024, 12, 31);
        var wageType = Wage(1, wageCode);
        var detail = Detail(1, wageType.Id, 1.2345678912345);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { detail },
            wageTypes: new[] { wageType },
            currencies: new[] { new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar", DecimalCount = 3 } });

        await service.ApplyRoundingAsync(
            Ref(forPeriod),
            new PayrollEngineSettings { Decimal6RoundingFrom = cutOver, DecimalInSalaryRegister = 2 });

        detail.Amount.ShouldBe(1.234568);
    }

    #endregion

    #region << GetCurrencyDecimalRounding (legacy Program.GetDecimalRounding, lines 3102-3121) >>

    [Fact]
    public async Task GetCurrencyDecimalRounding_Should_Default_To_Three_When_Nothing_Resolves()
    {
        // Legacy's local initialiser: Int16 decimalRounding = 3 (line 3104).
        var service = BuildService();

        (await service.GetCurrencyDecimalRoundingAsync(EmployeeId)).ShouldBe(3);
    }

    [Fact]
    public async Task GetCurrencyDecimalRounding_Should_Use_The_Employees_Payroll_Area_Currency()
    {
        var currency = new Currency { Id = 1, TenantId = 1, Code = "USD", NameEn = "Dollar", NameAr = "Dollar", DecimalCount = 2 };
        var payrollArea = new PayrollArea { Id = 1, TenantId = 1, Code = "PA1", Name = "Main", CurrencyId = currency.Id, Currency = currency };

        var service = BuildService(
            orgAssignments: new[]
            {
                new EmployeeOrgAssignment
                {
                    Id = 1, TenantId = 1, EmployeeId = EmployeeId,
                    StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2099, 12, 31),
                    OrganizationUnitId = OrganizationUnitId
                }
            },
            orgUnitProfiles: new[]
            {
                new OrgUnitProfile
                {
                    Id = 1, TenantId = 1,
                    OrganizationUnitId = OrganizationUnitId,
                    PayrollAreaId = payrollArea.Id,
                    PayrollArea = payrollArea
                }
            },
            currencies: new[] { currency });

        (await service.GetCurrencyDecimalRoundingAsync(EmployeeId)).ShouldBe(2);
    }

    [Fact]
    public async Task GetCurrencyDecimalRounding_Should_Fall_Back_To_The_Default_Currency()
    {
        // Legacy fallback: CurrencyService.GetDefaultCurrency(session).DecimalCount.
        var service = BuildService(
            currencies: new[] { new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar", DecimalCount = 5 } });

        (await service.GetCurrencyDecimalRoundingAsync(EmployeeId)).ShouldBe(5);
    }

    #endregion

    #region << GetNetPayRoundingDigits (legacy EmployeeCalculatedPayService.cs line 1076) >>

    [Fact]
    public async Task GetNetPayRoundingDigits_Should_Be_Two_When_RoundNetPay_Is_On()
    {
        var service = BuildService(
            currencies: new[] { new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar", DecimalCount = 5 } });

        (await service.GetNetPayRoundingDigitsAsync(EmployeeId, new PayrollEngineSettings { IsNetPayRound = true }))
            .ShouldBe(2);
    }

    [Fact]
    public async Task GetNetPayRoundingDigits_Should_Be_The_Currency_Decimals_When_RoundNetPay_Is_Off()
    {
        var service = BuildService(
            currencies: new[] { new Currency { Id = 1, TenantId = 1, Code = "KWD", NameEn = "Dinar", NameAr = "Dinar", DecimalCount = 5 } });

        (await service.GetNetPayRoundingDigitsAsync(EmployeeId, new PayrollEngineSettings { IsNetPayRound = false }))
            .ShouldBe(5);
    }

    [Fact]
    public async Task GetNetPayRoundingDigits_Should_Treat_Null_Settings_As_RoundNetPay_Off()
    {
        var service = BuildService();

        (await service.GetNetPayRoundingDigitsAsync(EmployeeId, null)).ShouldBe(3);
    }

    #endregion

    #region << Simulation target: rounds the simulation register, never the live one >>

    /// <summary>
    /// Wave P7: legacy <c>SimulationPayrollHelper.SetRoundoff</c> rounds the simulation register with the same
    /// digits. The live register with the same id must come out untouched.
    /// </summary>
    [Fact]
    public async Task ApplyRounding_Should_Round_The_Simulation_Register_And_Leave_The_Live_One_Alone()
    {
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var liveDetail = Detail(1, wageType.Id, 1.23456789);

        var simulation = new SimulationRegisterTestRepositories();
        simulation.Headers.Add(new SimulationSalaryRegister
        {
            Id = RegisterId, TenantId = 1, EmployeeId = EmployeeId, ForPeriod = forPeriod, InPeriod = forPeriod
        });
        simulation.Details.Add(new SimulationSalaryRegisterDetail
        {
            Id = 9, TenantId = 1, SimulationSalaryRegisterId = RegisterId, WageTypeId = wageType.Id,
            Rate = 9.87654321, Number = 9.87654321, Amount = 9.87654321
        });
        simulation.DayDetails.Add(new SimulationSalaryRegisterDaysDetail
        {
            Id = 3, TenantId = 1, SimulationSalaryRegisterDetailId = 9, Period = forPeriod, Amount = 9.87654321
        });

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { liveDetail },
            wageTypes: new[] { wageType });
        service.SimulationRows = simulation.BuildRows();

        await service.ApplyRoundingAsync(
            Ref(forPeriod, PayrollRegisterTarget.Simulation),
            new PayrollEngineSettings { DecimalInSalaryRegister = 2 });

        simulation.Details.Single().Amount.ShouldBe(9.88);
        simulation.Details.Single().Rate.ShouldBe(9.88);
        simulation.DayDetails.Single().Amount.ShouldBe(9.88);
        liveDetail.Amount.ShouldBe(1.23456789, "the live register must not have been touched");
    }

    /// <summary>
    /// Without the simulation tables the service refuses loudly -- legacy's catch-everything block must not
    /// swallow it into a silently un-rounded simulation, and the live register is never rounded instead.
    /// </summary>
    [Fact]
    public async Task ApplyRounding_Should_Refuse_A_Simulation_Register_Without_The_Simulation_Tables()
    {
        var forPeriod = new DateTime(2025, 3, 31);
        var wageType = Wage(1, "1000");
        var liveDetail = Detail(1, wageType.Id, 1.23456789);

        var service = BuildService(
            registers: new[] { Register(forPeriod) },
            details: new[] { liveDetail },
            wageTypes: new[] { wageType });

        await Should.ThrowAsync<PayrollRoundingService.SimulationTablesMissingException>(() => service.ApplyRoundingAsync(
            Ref(forPeriod, PayrollRegisterTarget.Simulation),
            new PayrollEngineSettings { DecimalInSalaryRegister = 2 }));

        liveDetail.Amount.ShouldBe(1.23456789, "the live register must not have been touched");
    }

    [Fact]
    public async Task ApplyRounding_Should_Be_A_No_Op_For_A_Null_Register()
    {
        var service = BuildService();

        await Should.NotThrowAsync(() => service.ApplyRoundingAsync(null, new PayrollEngineSettings()));
    }

    #endregion
}
