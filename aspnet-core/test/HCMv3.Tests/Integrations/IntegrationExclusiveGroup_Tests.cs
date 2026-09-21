using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using HCMv3.Integrations;
using HCMv3.Integrations.Dto;
using HCMv3.Integrations.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Integrations;

/// <summary>
/// Mutually exclusive integrations: sets where at most one member may be live at a time.
///
/// <para>
/// The case this exists for is the assistant's model provider -- Azure OpenAI, Bedrock, a self-hosted
/// Qwen. They are interchangeable answers to one question, and only one can be the answer: the assistant
/// sends each prompt to a single endpoint, and two enabled providers would leave that choice to whichever
/// happened to resolve first. A wrong-model answer is far harder to notice than an error, which is why
/// the rule is enforced on the server rather than by how the cards are drawn.
/// </para>
///
/// <para>
/// Tested with stand-in connectors rather than the five real ones, because none of those is in a group --
/// and because the rule has to hold for connectors that do not exist yet.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class IntegrationExclusiveGroup_Tests : AppTestBase
{
    private const string ProviderA = "FakeProviderA";
    private const string ProviderB = "FakeProviderB";
    private const string Standalone = "FakeStandalone";

    private readonly IIntegrationCenterAppService _integrationCenter;
    private readonly FakeConnectorState _state;

    public IntegrationExclusiveGroup_Tests()
    {
        LoginAsDefaultTenantAdmin();

        // Registered before the app service is resolved, so the registry picks them up when it is built.
        LocalIocManager.IocContainer.Register(
            Component.For<FakeConnectorState>().LifestyleSingleton(),
            Component.For<IIntegrationConnector>()
                     .ImplementedBy<FakeProviderAConnector>()
                     .Named(ProviderA)
                     .LifestyleTransient(),
            Component.For<IIntegrationConnector>()
                     .ImplementedBy<FakeProviderBConnector>()
                     .Named(ProviderB)
                     .LifestyleTransient(),
            Component.For<IIntegrationConnector>()
                     .ImplementedBy<FakeStandaloneConnector>()
                     .Named(Standalone)
                     .LifestyleTransient());

        _state = LocalIocManager.Resolve<FakeConnectorState>();
        _integrationCenter = Resolve<IIntegrationCenterAppService>();
    }

    [Fact]
    public async Task Should_Publish_The_Group_Name_So_The_Client_Can_Present_A_Choice()
    {
        var tiles = (await _integrationCenter.GetIntegrations()).Items;

        Tile(tiles, ProviderA).ExclusiveGroup.ShouldBe(IntegrationExclusiveGroups.AiProvider);
        Tile(tiles, ProviderB).ExclusiveGroup.ShouldBe(IntegrationExclusiveGroups.AiProvider);
        Tile(tiles, Standalone).ExclusiveGroup.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Switch_Off_A_Sibling_When_Another_Is_Switched_On()
    {
        await EnableAsync(ProviderA);
        await EnableAsync(ProviderB);

        _state.IsEnabled(ProviderB).ShouldBeTrue();
        _state.IsEnabled(ProviderA).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Switch_Back_Without_Losing_The_Other_Provider_Settings()
    {
        await EnableAsync(ProviderA);
        await EnableAsync(ProviderB);
        await EnableAsync(ProviderA);

        _state.IsEnabled(ProviderA).ShouldBeTrue();
        _state.IsEnabled(ProviderB).ShouldBeFalse();

        // The point of separate connectors rather than one card with a provider dropdown: switching away
        // and back is a toggle, not a re-entry of credentials.
        var tiles = (await _integrationCenter.GetIntegrations()).Items;
        Tile(tiles, ProviderB).IsConfigured.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Allow_Every_Member_Of_A_Group_To_Be_Off()
    {
        await EnableAsync(ProviderA);
        await DisableAsync(ProviderA);

        _state.IsEnabled(ProviderA).ShouldBeFalse();
        _state.IsEnabled(ProviderB).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Touch_Integrations_Outside_The_Group()
    {
        await EnableAsync(Standalone);
        await EnableAsync(ProviderA);
        await EnableAsync(ProviderB);

        // An unrelated integration must not be collateral damage of a provider switch.
        _state.IsEnabled(Standalone).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Show_A_Live_Integration_As_Failing_After_A_Failed_Test()
    {
        await EnableAsync(ProviderA);

        _state.NextTestSucceeds = false;
        await _integrationCenter.TestConnection(ProviderA);

        var tiles = (await _integrationCenter.GetIntegrations()).Items;
        Tile(tiles, ProviderA).Status.ShouldBe(IntegrationStatusType.Error);

        _state.NextTestSucceeds = true;
        await _integrationCenter.TestConnection(ProviderA);

        tiles = (await _integrationCenter.GetIntegrations()).Items;

        // Recovered: a card still red after a successful test sends someone chasing a fixed problem.
        Tile(tiles, ProviderA).Status.ShouldBe(IntegrationStatusType.Enabled);
    }

    [Fact]
    public async Task Should_Not_Show_A_Switched_Off_Integration_As_Failing()
    {
        await EnableAsync(ProviderA);

        _state.NextTestSucceeds = false;
        await _integrationCenter.TestConnection(ProviderA);

        await DisableAsync(ProviderA);

        var tiles = (await _integrationCenter.GetIntegrations()).Items;

        // Nothing is broken -- somebody turned it off deliberately. An alarm on a deliberate choice
        // teaches people to ignore alarms.
        Tile(tiles, ProviderA).Status.ShouldBe(IntegrationStatusType.Configured);
    }

    private Task EnableAsync(string key) =>
        _integrationCenter.SetEnabled(new SetIntegrationEnabledInput { Key = key, Enabled = true });

    private Task DisableAsync(string key) =>
        _integrationCenter.SetEnabled(new SetIntegrationEnabledInput { Key = key, Enabled = false });

    private static IntegrationTileDto Tile(IReadOnlyList<IntegrationTileDto> tiles, string key) =>
        tiles.Single(t => t.Key == key);
}

/// <summary>
/// Shared, test-scoped state for the stand-in connectors. Held in the container rather than in a static
/// field because connectors are transient -- a fresh instance is built every time the registry is -- and
/// because a static would leak between test classes running in parallel.
/// </summary>
public class FakeConnectorState
{
    private readonly ConcurrentDictionary<string, bool> _enabled = new();

    public bool NextTestSucceeds { get; set; } = true;

    public bool IsEnabled(string key) => _enabled.TryGetValue(key, out var value) && value;

    public void SetEnabled(string key, bool enabled) => _enabled[key] = enabled;
}

/// <summary>Stand-in connector: always configured, remembers whether it is switched on.</summary>
public abstract class FakeConnectorBase : IIntegrationConnector
{
    protected readonly FakeConnectorState State;

    protected FakeConnectorBase(FakeConnectorState state)
    {
        State = state;
    }

    public abstract string Key { get; }

    public abstract string ExclusiveGroup { get; }

    public IntegrationCategory Category => IntegrationCategory.Outbound;

    public string NameKey => "Integration_" + Key;

    public string DescriptionKey => "Integration_" + Key + "_Description";

    public IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>
    {
        new("ENDPOINT", "Integration_Fake_Endpoint", IntegrationFieldType.Url, false, false)
    };

    public bool SupportsTest => true;

    public bool SupportsEnableToggle => true;

    public string ExternalManageRoute => null;

    public Task<IntegrationConnectorState> GetStateAsync()
    {
        return Task.FromResult(new IntegrationConnectorState
        {
            IsConfigured = true,
            IsEnabled = State.IsEnabled(Key)
        });
    }

    public Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        return Task.FromResult((IReadOnlyList<IntegrationFieldValue>)new List<IntegrationFieldValue>
        {
            new("ENDPOINT", "https://fake.test", true)
        });
    }

    public Task SaveValuesAsync(IReadOnlyDictionary<string, string> values) => Task.CompletedTask;

    public Task<IntegrationTestResult> TestConnectionAsync()
    {
        return Task.FromResult(State.NextTestSucceeds
            ? IntegrationTestResult.Success("ok")
            : IntegrationTestResult.Failure("refused"));
    }

    public Task SetEnabledAsync(bool enabled)
    {
        State.SetEnabled(Key, enabled);
        return Task.CompletedTask;
    }
}

public class FakeProviderAConnector : FakeConnectorBase
{
    public FakeProviderAConnector(FakeConnectorState state) : base(state)
    {
    }

    public override string Key => "FakeProviderA";

    public override string ExclusiveGroup => IntegrationExclusiveGroups.AiProvider;
}

public class FakeProviderBConnector : FakeConnectorBase
{
    public FakeProviderBConnector(FakeConnectorState state) : base(state)
    {
    }

    public override string Key => "FakeProviderB";

    public override string ExclusiveGroup => IntegrationExclusiveGroups.AiProvider;
}

public class FakeStandaloneConnector : FakeConnectorBase
{
    public FakeStandaloneConnector(FakeConnectorState state) : base(state)
    {
    }

    public override string Key => "FakeStandalone";

    public override string ExclusiveGroup => null;
}
