using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Integrations;
using HCMv3.Integrations.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Integrations;

/// <summary>
/// The Integration Center, against the five real connectors.
///
/// <para>
/// The behaviour worth protecting hardest is that a saved credential never comes back out. The form has
/// to show that a password exists without showing the password, and it has to be able to save an
/// unrelated field without the blank password box wiping the stored one. Both are easy to break in a
/// refactor and neither is visible until someone's integration stops authenticating.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class IntegrationCenterAppService_Tests : AppTestBase
{
    private readonly IIntegrationCenterAppService _integrationCenter;

    public IntegrationCenterAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _integrationCenter = Resolve<IIntegrationCenterAppService>();
    }

    [Fact]
    public async Task Should_List_Every_Registered_Integration()
    {
        var result = await _integrationCenter.GetIntegrations();

        var keys = result.Items.Select(i => i.Key).ToList();

        keys.ShouldContain(IntegrationKeys.AzureSso);
        keys.ShouldContain(IntegrationKeys.Smtp);
        keys.ShouldContain(IntegrationKeys.D365Finance);
        keys.ShouldContain(IntegrationKeys.BioTime);
        keys.ShouldContain(IntegrationKeys.Webhooks);
    }

    [Fact]
    public async Task Should_Describe_Cards_With_Localization_Keys_Not_Text()
    {
        var result = await _integrationCenter.GetIntegrations();

        // The client localizes; sending finished English here would make the card ignore the user's
        // language.
        foreach (var tile in result.Items)
        {
            tile.NameKey.ShouldStartWith("Integration_");
            tile.DescriptionKey.ShouldStartWith("Integration_");
        }
    }

    [Fact]
    public async Task Should_Reject_An_Unknown_Integration()
    {
        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.GetIntegrationDetail("NoSuchIntegration"));
    }

    [Fact]
    public async Task Should_Report_D365_As_Not_Configured_On_A_Fresh_Tenant()
    {
        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance);

        detail.Tile.Status.ShouldBe(IntegrationStatusType.NotConfigured);
        detail.Tile.IsConfigured.ShouldBeFalse();
        detail.Fields.ShouldNotBeEmpty();
        detail.Fields.ShouldContain(f => f.Key == D365FinanceSettingKeys.ClientSecret && f.IsSecret);
    }

    [Fact]
    public async Task Should_Never_Return_A_Saved_Secret()
    {
        await SaveD365Async(clientSecret: "top-secret-value");

        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance);

        var secretField = detail.Fields.Single(f => f.Key == D365FinanceSettingKeys.ClientSecret);

        secretField.Value.ShouldBeNull();
        secretField.HasStoredValue.ShouldBeTrue();

        // And nothing else in the payload smuggles it out either.
        detail.Fields.ShouldNotContain(f => f.Value == "top-secret-value");
    }

    [Fact]
    public async Task Should_Keep_A_Stored_Secret_When_The_Form_Submits_It_Blank()
    {
        await SaveD365Async(clientSecret: "top-secret-value");

        // A second save, exactly as the form would send it: every field present, the secret box empty
        // because it never received the value in the first place.
        await SaveD365Async(clientSecret: "", resource: "https://changed.test");

        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance);

        detail.Fields.Single(f => f.Key == D365FinanceSettingKeys.Resource).Value.ShouldBe("https://changed.test");
        detail.Fields.Single(f => f.Key == D365FinanceSettingKeys.ClientSecret).HasStoredValue.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_To_Save_A_Half_Filled_Integration()
    {
        var input = new SaveIntegrationSettingsInput
        {
            Key = IntegrationKeys.D365Finance,
            Values = new Dictionary<string, string>
            {
                { D365FinanceSettingKeys.RequestUrl, "https://login.test/oauth2/token" }
                // every other required field left out
            }
        };

        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.SaveSettings(input));
    }

    [Fact]
    public async Task Should_Become_Configured_Once_Every_Required_Field_Is_Set()
    {
        await SaveD365Async(clientSecret: "top-secret-value");

        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance);

        detail.Tile.IsConfigured.ShouldBeTrue();

        // Configured is not the same as live: the tenant has to switch it on deliberately.
        detail.Tile.IsEnabled.ShouldBeFalse();
        detail.Tile.Status.ShouldBe(IntegrationStatusType.Configured);
    }

    [Fact]
    public async Task Should_Enable_And_Disable_A_Configured_Integration()
    {
        await SaveD365Async(clientSecret: "top-secret-value");

        await _integrationCenter.SetEnabled(new SetIntegrationEnabledInput
        {
            Key = IntegrationKeys.D365Finance,
            Enabled = true
        });

        (await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance))
            .Tile.Status.ShouldBe(IntegrationStatusType.Enabled);

        await _integrationCenter.SetEnabled(new SetIntegrationEnabledInput
        {
            Key = IntegrationKeys.D365Finance,
            Enabled = false
        });

        (await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance))
            .Tile.Status.ShouldBe(IntegrationStatusType.Configured);
    }

    [Fact]
    public async Task Should_Refuse_To_Enable_An_Unconfigured_Integration()
    {
        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.SetEnabled(new SetIntegrationEnabledInput
            {
                Key = IntegrationKeys.D365Finance,
                Enabled = true
            }));
    }

    [Fact]
    public async Task Should_Record_Every_Connection_Test()
    {
        // Unconfigured, so this fails without reaching the network -- the point here is that the attempt
        // is written down, which is what lets a card say when the integration last worked.
        var result = await _integrationCenter.TestConnection(IntegrationKeys.D365Finance);

        result.Succeeded.ShouldBeFalse();

        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.D365Finance);

        detail.RecentAttempts.ShouldNotBeEmpty();
        detail.RecentAttempts.First().AttemptType.ShouldBe(IntegrationAttemptType.Test);
        detail.RecentAttempts.First().Succeeded.ShouldBeFalse();
        detail.Tile.LastTestSucceeded.ShouldBe(false);
    }

    [Fact]
    public async Task Should_Not_Offer_A_Form_Or_A_Switch_For_Webhooks()
    {
        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.Webhooks);

        // Webhooks are one-to-many subscriptions; a flat field list cannot express them, so the card
        // sends the administrator to the screen that can.
        detail.Fields.ShouldBeEmpty();
        detail.Tile.HasFields.ShouldBeFalse();
        detail.Tile.ExternalManageRoute.ShouldNotBeNullOrWhiteSpace();
        detail.Tile.SupportsEnableToggle.ShouldBeFalse();
        detail.Tile.SupportsTest.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Refuse_Operations_An_Integration_Does_Not_Support()
    {
        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.SetEnabled(new SetIntegrationEnabledInput
            {
                Key = IntegrationKeys.Webhooks,
                Enabled = true
            }));

        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.TestConnection(IntegrationKeys.Webhooks));

        await Should.ThrowAsync<UserFriendlyException>(
            async () => await _integrationCenter.SaveSettings(new SaveIntegrationSettingsInput
            {
                Key = IntegrationKeys.Webhooks,
                Values = new Dictionary<string, string>()
            }));
    }

    [Fact]
    public async Task Should_Offer_A_Real_Switch_For_Smtp()
    {
        var detail = await _integrationCenter.GetIntegrationDetail(IntegrationKeys.Smtp);

        // It writes a flag the sender checks before every send -- not a display-only toggle.
        detail.Tile.SupportsEnableToggle.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_To_Test_Smtp_While_Outbound_Email_Is_Switched_Off()
    {
        await _integrationCenter.SetEnabled(new SetIntegrationEnabledInput
        {
            Key = IntegrationKeys.Smtp,
            Enabled = false
        });

        var result = await _integrationCenter.TestConnection(IntegrationKeys.Smtp);

        // The test goes through the gated sender, which would drop the message. Reporting success here
        // would tell an administrator their mail server works when nothing was sent at all.
        result.Succeeded.ShouldBeFalse();
    }

    private async Task SaveD365Async(string clientSecret, string resource = "https://finance.test")
    {
        await _integrationCenter.SaveSettings(new SaveIntegrationSettingsInput
        {
            Key = IntegrationKeys.D365Finance,
            Values = new Dictionary<string, string>
            {
                { D365FinanceSettingKeys.RequestUrl, "https://login.test/oauth2/token" },
                { D365FinanceSettingKeys.ClientId, "client-id" },
                { D365FinanceSettingKeys.ClientSecret, clientSecret },
                { D365FinanceSettingKeys.GrantType, "client_credentials" },
                { D365FinanceSettingKeys.Resource, resource },
                { D365FinanceSettingKeys.RequestUrlJournalHeader, "https://finance.test/header" },
                { D365FinanceSettingKeys.RequestUrlJournalLine, "https://finance.test/line" },
                { D365FinanceSettingKeys.JournalHeaderODataETag, "etag" },
                { D365FinanceSettingKeys.ChangesetLimit, "100" }
            }
        });
    }
}
