using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.SetupChecks;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class SetupCheckRunner_Tests : TenantSetupCheckTestBase
{
    private readonly SetupCheckRunner _runner;

    public SetupCheckRunner_Tests()
    {
        LoginAsHostAdmin();
        LocalIocManager.Register<WritingCheck>(DependencyLifeStyle.Transient);
        _runner = Resolve<SetupCheckRunner>();
    }

    private static SetupCheckInfo Info<TCheck>(string code) =>
        new(typeof(TCheck), code, V310, SetupCheckSeverity.Info, "SGM015");

    [Fact]
    public async Task Should_Throw_Away_Anything_A_Check_Writes()
    {
        var tenantId = CreateEmptyTenant("readonly");

        var runs = await _runner.RunAsync(tenantId, new[] { Info<WritingCheck>("TST-010") }, DateTime.Today);

        runs.Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Ok);
        UsingDbContext(tenantId, context => context.Set<Currency>().Count()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Report_A_Failing_Check_And_Still_Run_The_Rest()
    {
        var tenantId = CreateEmptyTenant("failing");

        var runs = await _runner.RunByCodesAsync(tenantId, new[] { ThrowingCheck.CheckCode, CurrencyExistsCheck.CheckCode }, DateTime.Today);

        runs[0].Finding.Outcome.ShouldBe(SetupCheckOutcome.Error);
        runs[0].Finding.Message.ShouldBe("boom");
        runs[1].Finding.Outcome.ShouldBe(SetupCheckOutcome.Missing);
    }

    [Fact]
    public async Task Should_Run_Each_Check_Inside_The_Chosen_Tenant()
    {
        var withCurrency = CreateEmptyTenant("withcurrency");
        AddCurrency(withCurrency);
        var withoutCurrency = CreateEmptyTenant("withoutcurrency");
        var codes = new[] { CurrencyExistsCheck.CheckCode };

        (await _runner.RunByCodesAsync(withCurrency, codes, DateTime.Today)).Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Ok);
        (await _runner.RunByCodesAsync(withoutCurrency, codes, DateTime.Today)).Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Missing);
    }

    [Fact]
    public async Task Should_Skip_Codes_That_Are_Not_Registered()
    {
        var tenantId = CreateEmptyTenant("unknowncodes");

        var runs = await _runner.RunByCodesAsync(tenantId, new[] { "TST-999", CurrencyExistsCheck.CheckCode }, DateTime.Today);

        runs.Select(r => r.Check.Code).ShouldBe(new[] { CurrencyExistsCheck.CheckCode });
    }

    /// <summary>A check that misbehaves by writing; the runner must roll it back.</summary>
    public class WritingCheck : ISetupCheck
    {
        private readonly IRepository<Currency> _currencies;

        public WritingCheck(IRepository<Currency> currencies)
        {
            _currencies = currencies;
        }

        public string Code => "TST-010";
        public string IntroducedIn => V310;
        public SetupCheckSeverity Severity => SetupCheckSeverity.Info;
        public string FixScreenCode => "SGM015";

        public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
        {
            await _currencies.InsertAndGetIdAsync(new Currency { Code = "SNEAKY", NameEn = "SNEAKY", NameAr = "SNEAKY" });
            return SetupCheckFinding.Exists(true);
        }
    }
}
