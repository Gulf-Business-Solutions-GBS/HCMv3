using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Integrations.Dto;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations;

/// <inheritdoc cref="IIntegrationCenterAppService"/>
/// <remarks>
/// This service owns no storage. Each <see cref="IIntegrationConnector"/> knows where its own settings
/// live -- ABP settings, a domain entity, or <see cref="IntegrationSetting"/> -- and this service only
/// orchestrates: it derives the badge every card shows, enforces exclusivity between connectors, and
/// records what happened.
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_IntegrationCenter)]
public class IntegrationCenterAppService : HCMv3AppServiceBase, IIntegrationCenterAppService
{
    private const int RecentAttemptCount = 10;

    private readonly IIntegrationConnectorRegistry _registry;
    private readonly IIntegrationConnectionLogger _connectionLogger;

    public IntegrationCenterAppService(
        IIntegrationConnectorRegistry registry,
        IIntegrationConnectionLogger connectionLogger)
    {
        _registry = registry;
        _connectionLogger = connectionLogger;
    }

    public async Task<ListResultDto<IntegrationTileDto>> GetIntegrations()
    {
        // One query for every card's history, rather than one per card.
        var latestByIntegration = await _connectionLogger.GetLatestForAllAsync();

        var tiles = new List<IntegrationTileDto>();

        foreach (var connector in _registry.GetAll())
        {
            var state = await connector.GetStateAsync();

            latestByIntegration.TryGetValue(connector.Key, out var latest);

            tiles.Add(BuildTile(connector, state, latest));
        }

        return new ListResultDto<IntegrationTileDto>(tiles);
    }

    public async Task<IntegrationDetailDto> GetIntegrationDetail(string key)
    {
        var connector = RequireConnector(key);

        var state = await connector.GetStateAsync();
        var values = await connector.GetValuesAsync();
        var latest = await _connectionLogger.GetLatestAsync(connector.Key);
        var recent = await _connectionLogger.GetRecentAsync(connector.Key, RecentAttemptCount);

        var valuesByKey = values.ToDictionary(v => v.Key, StringComparer.OrdinalIgnoreCase);

        return new IntegrationDetailDto
        {
            Tile = BuildTile(connector, state, latest),
            Fields = connector.Fields.Select(f => ToFieldDto(f, valuesByKey)).ToList(),
            RecentAttempts = recent.Select(ToAttemptDto).ToList()
        };
    }

    /// <summary>
    /// Behind its own permission: saving a form means setting the credentials an integration signs in
    /// with, which is a stricter thing than being allowed to see what is connected.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Administration_IntegrationCenter_ManageSecrets)]
    public async Task SaveSettings(SaveIntegrationSettingsInput input)
    {
        var connector = RequireConnector(input.Key);

        if (!connector.Fields.Any())
        {
            throw new UserFriendlyException(L("IntegrationHasNoFields", L(connector.NameKey)));
        }

        await connector.SaveValuesAsync(
            input.Values ?? new Dictionary<string, string>());
    }

    /// <summary>
    /// Exclusivity is enforced here rather than in the browser, because the card grid is only one of the
    /// ways this endpoint can be reached. Siblings are switched off before the target is switched on, so
    /// a failure part-way leaves nothing enabled rather than two things enabled.
    /// </summary>
    [UnitOfWork]
    public virtual async Task SetEnabled(SetIntegrationEnabledInput input)
    {
        var connector = RequireConnector(input.Key);

        if (!connector.SupportsEnableToggle)
        {
            throw new UserFriendlyException(L("IntegrationHasNoSwitch", L(connector.NameKey)));
        }

        if (input.Enabled && !string.IsNullOrEmpty(connector.ExclusiveGroup))
        {
            foreach (var sibling in SiblingsOf(connector))
            {
                var siblingState = await sibling.GetStateAsync();
                if (siblingState.IsEnabled)
                {
                    await sibling.SetEnabledAsync(false);
                }
            }
        }

        await connector.SetEnabledAsync(input.Enabled);
    }

    public async Task<IntegrationTestResultDto> TestConnection(string key)
    {
        var connector = RequireConnector(key);

        if (!connector.SupportsTest)
        {
            throw new UserFriendlyException(L("IntegrationHasNoTest", L(connector.NameKey)));
        }

        IntegrationTestResult result;

        try
        {
            result = await connector.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            // A connector is meant to return a failure rather than throw, but an unexpected exception
            // must still be recorded and reported -- not swallowed into a blank card.
            result = IntegrationTestResult.Failure(ex.Message);
        }

        await _connectionLogger.LogAsync(
            connector.Key,
            IntegrationAttemptType.Test,
            result.Succeeded,
            result.Message,
            result.DurationMs);

        return new IntegrationTestResultDto
        {
            Succeeded = result.Succeeded,
            Message = result.Message,
            DurationMs = result.DurationMs
        };
    }

    private IEnumerable<IIntegrationConnector> SiblingsOf(IIntegrationConnector connector)
    {
        return _registry.GetAll()
            .Where(c => string.Equals(c.ExclusiveGroup, connector.ExclusiveGroup, StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c.Key, connector.Key, StringComparison.OrdinalIgnoreCase));
    }

    private IIntegrationConnector RequireConnector(string key)
    {
        var connector = _registry.Find(key);
        if (connector == null)
        {
            throw new UserFriendlyException(L("IntegrationNotFound", key));
        }

        return connector;
    }

    private static IntegrationTileDto BuildTile(
        IIntegrationConnector connector,
        IntegrationConnectorState state,
        IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog> latest)
    {
        latest ??= new Dictionary<IntegrationAttemptType, IntegrationConnectionLog>();

        latest.TryGetValue(IntegrationAttemptType.Test, out var lastTest);
        latest.TryGetValue(IntegrationAttemptType.Sync, out var lastSync);

        return new IntegrationTileDto
        {
            Key = connector.Key,
            NameKey = connector.NameKey,
            DescriptionKey = connector.DescriptionKey,
            Category = connector.Category,
            Status = DeriveStatus(state, lastTest, lastSync),
            IsConfigured = state.IsConfigured,
            IsEnabled = state.IsEnabled,
            StatusDetail = state.Detail,
            SupportsTest = connector.SupportsTest,
            SupportsEnableToggle = connector.SupportsEnableToggle,
            ExclusiveGroup = connector.ExclusiveGroup,
            ExternalManageRoute = connector.ExternalManageRoute,
            HasFields = connector.Fields.Any(),
            LastTestTime = lastTest?.CreationTime,
            LastTestSucceeded = lastTest?.Succeeded,
            LastTestMessage = lastTest?.Message,
            LastSyncTime = lastSync?.CreationTime,
            LastSyncSucceeded = lastSync?.Succeeded,
            LastSyncItemCount = lastSync?.ItemCount
        };
    }

    /// <summary>
    /// The badge, derived in one place so five connectors cannot disagree about what the same three
    /// states look like.
    ///
    /// A failure only shows as <see cref="IntegrationStatusType.Error"/> while it is the most recent thing
    /// that happened: a test that failed this morning and a sync that succeeded since means the
    /// integration is working, and a card still showing red would send someone chasing a fixed problem.
    ///
    /// A switched-off integration never shows red either, whatever its last attempt did. Nothing is
    /// broken -- somebody turned it off on purpose -- and an alarm on a deliberate choice trains people
    /// to ignore alarms.
    /// </summary>
    private static IntegrationStatusType DeriveStatus(
        IntegrationConnectorState state,
        IntegrationConnectionLog lastTest,
        IntegrationConnectionLog lastSync)
    {
        if (!state.IsConfigured)
        {
            return IntegrationStatusType.NotConfigured;
        }

        if (!state.IsEnabled)
        {
            return IntegrationStatusType.Configured;
        }

        var newest = Newest(lastTest, lastSync);
        if (newest != null && !newest.Succeeded)
        {
            return IntegrationStatusType.Error;
        }

        return IntegrationStatusType.Enabled;
    }

    private static IntegrationConnectionLog Newest(IntegrationConnectionLog a, IntegrationConnectionLog b)
    {
        if (a == null)
        {
            return b;
        }

        if (b == null)
        {
            return a;
        }

        return a.CreationTime >= b.CreationTime ? a : b;
    }

    private static IntegrationFieldDto ToFieldDto(
        IntegrationFieldDefinition definition,
        IReadOnlyDictionary<string, IntegrationFieldValue> valuesByKey)
    {
        valuesByKey.TryGetValue(definition.Key, out var value);

        return new IntegrationFieldDto
        {
            Key = definition.Key,
            LabelKey = definition.LabelKey,
            HelpKey = definition.HelpKey,
            Type = definition.Type,
            IsRequired = definition.IsRequired,
            IsSecret = definition.IsSecret,

            // Belt and braces: the connectors already withhold secrets, and this makes it impossible for
            // a future connector to leak one through this endpoint by forgetting to.
            Value = definition.IsSecret ? null : value?.Value,
            HasStoredValue = value?.HasStoredValue ?? false,
            DefaultValue = definition.DefaultValue
        };
    }

    private static IntegrationAttemptDto ToAttemptDto(IntegrationConnectionLog log)
    {
        return new IntegrationAttemptDto
        {
            Time = log.CreationTime,
            AttemptType = log.AttemptType,
            Succeeded = log.Succeeded,
            Message = log.Message,
            ItemCount = log.ItemCount,
            DurationMs = log.DurationMs
        };
    }
}
