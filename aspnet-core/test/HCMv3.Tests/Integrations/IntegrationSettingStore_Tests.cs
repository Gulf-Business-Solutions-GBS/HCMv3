using System.Linq;
using System.Threading.Tasks;
using HCMv3.Integrations;
using HCMv3.Integrations.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Integrations;

/// <summary>
/// The store behind the Integration Center.
///
/// <para>
/// The behaviour worth protecting hardest is that a secret is unreadable in the database but readable to
/// the code that needs it. Both halves matter: encryption that cannot be reversed breaks every
/// integration at once, and a "secret" sitting in plain text is the problem this table was created to
/// fix.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class IntegrationSettingStore_Tests : AppTestBase
{
    private const string TestIntegration = "TestIntegration";

    private readonly IIntegrationSettingStore _store;

    public IntegrationSettingStore_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _store = Resolve<IIntegrationSettingStore>();
    }

    [Fact]
    public async Task Should_Return_Null_When_Nothing_Stored()
    {
        var value = await _store.GetAsync(TestIntegration, "ANY_KEY");

        value.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Round_Trip_A_Plain_Value()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://example.test/api", isSecret: false);

        var value = await _store.GetAsync(TestIntegration, "ENDPOINT");

        value.ShouldBe("https://example.test/api");
    }

    [Fact]
    public async Task Should_Round_Trip_A_Secret()
    {
        await _store.SetAsync(TestIntegration, "CLIENT_SECRET", "s3cr3t-value", isSecret: true);

        var value = await _store.GetAsync(TestIntegration, "CLIENT_SECRET");

        value.ShouldBe("s3cr3t-value");
    }

    [Fact]
    public async Task Should_Not_Store_A_Secret_In_Readable_Form()
    {
        await _store.SetAsync(TestIntegration, "CLIENT_SECRET", "s3cr3t-value", isSecret: true);

        // Read the column directly: what the store returns is not the question here, what an admin
        // browsing the table would see is.
        string stored = null;
        UsingDbContext(context =>
        {
            stored = context.Set<IntegrationSetting>()
                .Single(s => s.IntegrationKey == TestIntegration && s.SettingKey == "CLIENT_SECRET")
                .Value;
        });

        stored.ShouldNotBeNullOrWhiteSpace();
        stored.ShouldNotBe("s3cr3t-value");
        stored.ShouldNotContain("s3cr3t");
    }

    [Fact]
    public async Task Should_Leave_A_Plain_Value_Readable()
    {
        await _store.SetAsync(TestIntegration, "GRANT_TYPE", "client_credentials", isSecret: false);

        string stored = null;
        UsingDbContext(context =>
        {
            stored = context.Set<IntegrationSetting>()
                .Single(s => s.IntegrationKey == TestIntegration && s.SettingKey == "GRANT_TYPE")
                .Value;
        });

        // Non-secret values stay legible on purpose: an endpoint URL is far easier to support when it can
        // be read straight out of the table.
        stored.ShouldBe("client_credentials");
    }

    [Fact]
    public async Task Should_Overwrite_Rather_Than_Duplicate()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://first.test", isSecret: false);
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://second.test", isSecret: false);

        (await _store.GetAsync(TestIntegration, "ENDPOINT")).ShouldBe("https://second.test");

        var rowCount = 0;
        UsingDbContext(context =>
        {
            rowCount = context.Set<IntegrationSetting>()
                .Count(s => s.IntegrationKey == TestIntegration && s.SettingKey == "ENDPOINT");
        });

        rowCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Remove_The_Row_When_Set_To_Blank()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://example.test", isSecret: false);
        await _store.SetAsync(TestIntegration, "ENDPOINT", "   ", isSecret: false);

        (await _store.GetAsync(TestIntegration, "ENDPOINT")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Decrypt_Every_Secret_In_A_Bulk_Read()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://example.test", isSecret: false);
        await _store.SetAsync(TestIntegration, "CLIENT_SECRET", "s3cr3t-value", isSecret: true);

        var all = await _store.GetAllAsync(TestIntegration);

        all["ENDPOINT"].ShouldBe("https://example.test");
        all["CLIENT_SECRET"].ShouldBe("s3cr3t-value");
    }

    [Fact]
    public async Task Should_Report_Which_Keys_Have_A_Value()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://example.test", isSecret: false);
        await _store.SetAsync(TestIntegration, "CLIENT_SECRET", "s3cr3t-value", isSecret: true);

        var keys = await _store.GetKeysWithValueAsync(TestIntegration);

        // This is how the form shows "a password is saved" without the password crossing to the browser.
        keys.ShouldContain("ENDPOINT");
        keys.ShouldContain("CLIENT_SECRET");
    }

    [Fact]
    public async Task Should_Keep_Integrations_Apart()
    {
        await _store.SetAsync("IntegrationA", "ENDPOINT", "https://a.test", isSecret: false);
        await _store.SetAsync("IntegrationB", "ENDPOINT", "https://b.test", isSecret: false);

        (await _store.GetAsync("IntegrationA", "ENDPOINT")).ShouldBe("https://a.test");
        (await _store.GetAsync("IntegrationB", "ENDPOINT")).ShouldBe("https://b.test");
    }

    [Fact]
    public async Task Should_Remove_Every_Setting_Of_One_Integration()
    {
        await _store.SetAsync(TestIntegration, "ENDPOINT", "https://example.test", isSecret: false);
        await _store.SetAsync(TestIntegration, "CLIENT_SECRET", "s3cr3t-value", isSecret: true);
        await _store.SetAsync("OtherIntegration", "ENDPOINT", "https://other.test", isSecret: false);

        await _store.RemoveAllAsync(TestIntegration);

        (await _store.GetAllAsync(TestIntegration)).ShouldBeEmpty();
        (await _store.GetAsync("OtherIntegration", "ENDPOINT")).ShouldBe("https://other.test");
    }
}
