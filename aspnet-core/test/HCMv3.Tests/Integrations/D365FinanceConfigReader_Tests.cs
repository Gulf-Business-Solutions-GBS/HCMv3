using System;
using System.Threading.Tasks;
using HCMv3.Integrations;
using HCMv3.Integrations.Services;
using HCMv3.SystemSecurity;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Integrations;

/// <summary>
/// Where the Dynamics 365 Finance settings are read from while they are mid-move.
///
/// <para>
/// The behaviour worth protecting hardest is the precedence. A tenant who has just re-entered their
/// credentials in the Integration Center must post with the new ones; if the legacy table won instead,
/// posting would quietly carry on with the old credentials and the fault would surface weeks later as a
/// finance reconciliation problem rather than as an error anyone could act on.
/// </para>
///
/// <para>
/// The fallback itself matters just as much in the other direction: tenants configured before the
/// Integration Center existed still have plain rows in <c>SysTenantConfig</c>, and payroll posting has to
/// keep working for them until the one-off migration moves their keys across.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class D365FinanceConfigReader_Tests : AppTestBase
{
    private readonly ID365FinanceConfigReader _reader;
    private readonly IIntegrationSettingStore _store;

    public D365FinanceConfigReader_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _reader = Resolve<ID365FinanceConfigReader>();
        _store = Resolve<IIntegrationSettingStore>();
    }

    [Fact]
    public async Task Should_Return_Null_When_Neither_Store_Has_It()
    {
        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Read_From_The_Integration_Center()
    {
        await _store.SetAsync(IntegrationKeys.D365Finance, D365FinanceSettingKeys.RequestUrl,
            "https://new.test/token", isSecret: false);

        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBe("https://new.test/token");
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Legacy_Table()
    {
        GivenLegacyConfig(D365FinanceSettingKeys.RequestUrl, "https://legacy.test/token");

        // A tenant set up before the Integration Center existed must keep posting.
        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBe("https://legacy.test/token");
    }

    [Fact]
    public async Task Should_Prefer_The_Integration_Center_Over_The_Legacy_Table()
    {
        GivenLegacyConfig(D365FinanceSettingKeys.RequestUrl, "https://legacy.test/token");

        await _store.SetAsync(IntegrationKeys.D365Finance, D365FinanceSettingKeys.RequestUrl,
            "https://new.test/token", isSecret: false);

        // Re-entering credentials in the Integration Center has to take effect immediately. Posting on
        // with the superseded ones is the failure this test exists to prevent.
        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBe("https://new.test/token");
    }

    [Fact]
    public async Task Should_Fall_Back_Per_Key_Rather_Than_All_Or_Nothing()
    {
        GivenLegacyConfig(D365FinanceSettingKeys.RequestUrl, "https://legacy.test/token");
        GivenLegacyConfig(D365FinanceSettingKeys.ClientId, "legacy-client");

        await _store.SetAsync(IntegrationKeys.D365Finance, D365FinanceSettingKeys.ClientId,
            "new-client", isSecret: false);

        // A half-migrated tenant is a real state, and it has to work rather than half-work.
        (await _reader.GetAsync(D365FinanceSettingKeys.ClientId)).ShouldBe("new-client");
        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBe("https://legacy.test/token");
    }

    [Fact]
    public async Task Should_Decrypt_A_Secret_Held_By_The_Integration_Center()
    {
        await _store.SetAsync(IntegrationKeys.D365Finance, D365FinanceSettingKeys.ClientSecret,
            "s3cr3t-value", isSecret: true);

        // Encrypted at rest, plain text to the code that has to authenticate with it.
        (await _reader.GetAsync(D365FinanceSettingKeys.ClientSecret)).ShouldBe("s3cr3t-value");
    }

    [Fact]
    public async Task Should_Treat_A_Blank_Legacy_Row_As_Missing()
    {
        GivenLegacyConfig(D365FinanceSettingKeys.RequestUrl, "");

        // GetSysTenantConfigAsync returns "" rather than null for a missing row, so blank has to count as
        // missing or callers get an empty string instead of a clear "not configured" error.
        (await _reader.GetAsync(D365FinanceSettingKeys.RequestUrl)).ShouldBeNull();
    }

    private void GivenLegacyConfig(string key, string value)
    {
        UsingDbContext(context =>
        {
            context.Set<SysTenantConfig>().Add(new SysTenantConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Key = key,
                KeyValue = value,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                ConfigCategory = TenantConfigCategory.Payroll
            });
        });
    }
}
