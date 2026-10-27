using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Integrations;
using HCMv3.Integrations.Connectors;
using HCMv3.Integrations.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Integrations;

/// <summary>
/// The Integration Center on/off switch as Post JV reads it. Before this, the D365 card's switch was never
/// consulted by the posting path.
/// </summary>
// ReSharper disable once InconsistentNaming
public class D365FinancePostingSwitch_Tests
{
    private readonly IIntegrationSettingStore _store = Substitute.For<IIntegrationSettingStore>();

    private D365FinancePostingSwitch Create(string flag, params string[] storedKeys)
    {
        _store.GetAsync(IntegrationKeys.D365Finance, StoreBackedIntegrationConnectorBase.EnabledSettingKey).Returns(flag);
        _store.GetKeysWithValueAsync(IntegrationKeys.D365Finance)
            .Returns((IReadOnlyCollection<string>)new List<string>(storedKeys));
        return new D365FinancePostingSwitch(_store);
    }

    [Fact]
    public async Task Should_Allow_Posting_When_Switched_On()
    {
        (await Create("true", D365FinanceSettingKeys.ClientId).IsPostingAllowedAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Posting_When_Switched_Off()
    {
        (await Create("false", D365FinanceSettingKeys.ClientId).IsPostingAllowedAsync()).ShouldBeFalse();
    }

    /// <summary>Set up in the Integration Center but never switched on: the card says "switched off", so posting agrees.</summary>
    [Fact]
    public async Task Should_Refuse_Posting_When_Set_Up_In_The_Integration_Center_But_Never_Switched_On()
    {
        (await Create(null, D365FinanceSettingKeys.ClientId, D365FinanceSettingKeys.RequestUrl).IsPostingAllowedAsync())
            .ShouldBeFalse();
    }

    /// <summary>A tenant still configured only in SysTenantConfig (before the Integration Center) keeps posting.</summary>
    [Fact]
    public async Task Should_Allow_Posting_For_A_Tenant_Configured_Before_The_Integration_Center()
    {
        (await Create(null).IsPostingAllowedAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Pre_Integration_Center_Tenant_Once_The_Card_Is_Switched_Off()
    {
        (await Create("false").IsPostingAllowedAsync()).ShouldBeFalse();
    }
}
