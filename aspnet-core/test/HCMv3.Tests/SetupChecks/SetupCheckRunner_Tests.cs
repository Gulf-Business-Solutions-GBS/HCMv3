using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.Releases;
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
        LocalIocManager.Register<ThrowingCheck>(DependencyLifeStyle.Transient);
        _runner = Resolve<SetupCheckRunner>();
    }

    private static SetupCheckInfo Info<TCheck>(string code) =>
        new(typeof(TCheck), code, SetupCheckModules.Payroll, HCMv3Releases.V3_0_0, SetupCheckSeverity.Info, "SPY025");

    [Fact]
    public async Task Should_Throw_Away_Anything_A_Check_Writes()
    {
        var tenantId = CreateEmptyTenant("readonly");

        var runs = await _runner.RunAsync(tenantId, new[] { Info<WritingCheck>("TST-001") }, DateTime.Today);

        runs.Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Ok);
        UsingDbContext(tenantId, context => context.Set<Currency>().Count()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Report_A_Failing_Check_And_Still_Run_The_Rest()
    {
        var tenantId = CreateEmptyTenant("failing");

        var runs = await _runner.RunAsync(
            tenantId,
            new[] { Info<ThrowingCheck>("TST-002"), Info<PayrollAreaExistsCheck>("PAY-001") },
            DateTime.Today);

        runs[0].Finding.Outcome.ShouldBe(SetupCheckOutcome.Error);
        runs[0].Finding.Message.ShouldBe("boom");
        runs[1].Finding.Outcome.ShouldBe(SetupCheckOutcome.Missing);
    }

    [Fact]
    public async Task Should_Run_Each_Check_Inside_The_Chosen_Tenant()
    {
        var withArea = CreateEmptyTenant("witharea");
        CreatePayrollArea(withArea, "MAIN");
        var withoutArea = CreateEmptyTenant("withoutarea");
        var check = new[] { Info<PayrollAreaExistsCheck>("PAY-001") };

        (await _runner.RunAsync(withArea, check, DateTime.Today)).Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Ok);
        (await _runner.RunAsync(withoutArea, check, DateTime.Today)).Single().Finding.Outcome.ShouldBe(SetupCheckOutcome.Missing);
    }

    [Fact]
    public void Should_Choose_Checks_By_Release()
    {
        _runner.GetChecksFor(HCMv3Releases.V3_0_0, onlyThisVersion: true).Count.ShouldBe(SetupCheckCatalog.All.Count);
        _runner.GetChecksFor(HCMv3Releases.V3_0_0, onlyThisVersion: false).Count.ShouldBe(SetupCheckCatalog.All.Count);
    }

    /// <summary>A check that misbehaves by writing; the runner must roll it back.</summary>
    public class WritingCheck : ISetupCheck
    {
        private readonly IRepository<Currency> _currencies;

        public WritingCheck(IRepository<Currency> currencies)
        {
            _currencies = currencies;
        }

        public string Code => "TST-001";
        public string Module => SetupCheckModules.Payroll;
        public string IntroducedIn => HCMv3Releases.V3_0_0;
        public SetupCheckSeverity Severity => SetupCheckSeverity.Info;
        public string FixScreenCode => "SPY025";

        public async Task<SetupCheckFinding> RunAsync(SetupCheckContext context)
        {
            await _currencies.InsertAndGetIdAsync(new Currency { Code = "SNEAKY", NameEn = "SNEAKY", NameAr = "SNEAKY" });
            return SetupCheckFinding.Exists(true);
        }
    }

    public class ThrowingCheck : ISetupCheck
    {
        public string Code => "TST-002";
        public string Module => SetupCheckModules.Payroll;
        public string IntroducedIn => HCMv3Releases.V3_0_0;
        public SetupCheckSeverity Severity => SetupCheckSeverity.Info;
        public string FixScreenCode => "SPY025";

        public Task<SetupCheckFinding> RunAsync(SetupCheckContext context) => throw new InvalidOperationException("boom");
    }
}
