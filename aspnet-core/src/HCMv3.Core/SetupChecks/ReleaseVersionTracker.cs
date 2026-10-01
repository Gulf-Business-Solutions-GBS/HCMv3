using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.Configuration;
using HCMv3.Releases;

namespace HCMv3.SetupChecks;

/// <summary>Which release a tenant is on, and which it was on before the latest update.</summary>
public record ReleaseVersionState(string CurrentVersion, string PreviousVersion, DateTime? UpdatedOn)
{
    /// <summary>False until an update has happened since tracking began: there is nothing to compare with.</summary>
    public bool HasComparison => !string.IsNullOrEmpty(PreviousVersion);
}

/// <summary>
/// Remembers, per tenant, the last HCMv3 release it saw, so an update can be told apart from an ordinary visit.
///
/// <para>Kept in three tenant settings (<see cref="AppSettings.Releases"/>). The first time a tenant is seen the
/// current release is recorded and there is nothing to compare. When the code's release later differs from the one
/// recorded, the recorded one becomes the PREVIOUS release and the current one is recorded -- so the comparison
/// "previous to current" stays on screen until the next update, however many times the page is opened.</para>
/// </summary>
public class ReleaseVersionTracker : HCMv3DomainServiceBase
{
    private readonly IReleaseCatalog _catalog;

    public ReleaseVersionTracker(IReleaseCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<ReleaseVersionState> RecordAsync(int tenantId)
    {
        var current = _catalog.Releases[^1].Version;
        var lastSeen = await SettingManager.GetSettingValueForTenantAsync(AppSettings.Releases.LastSeenVersion, tenantId);

        if (string.IsNullOrWhiteSpace(lastSeen))
        {
            // First sight: record where tracking starts; no previous release to compare with.
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.LastSeenVersion, current);
        }
        else if (!string.Equals(lastSeen.Trim(), current, StringComparison.Ordinal))
        {
            // The code moved on since the tenant was last seen: that is an update.
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.PreviousVersion, lastSeen.Trim());
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.LastSeenVersion, current);
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId, AppSettings.Releases.UpdatedOn, Clock.Now.ToString("o", CultureInfo.InvariantCulture));
        }

        var previous = await SettingManager.GetSettingValueForTenantAsync(AppSettings.Releases.PreviousVersion, tenantId);
        var updatedOnText = await SettingManager.GetSettingValueForTenantAsync(AppSettings.Releases.UpdatedOn, tenantId);
        DateTime? updatedOn = DateTime.TryParse(updatedOnText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;

        return new ReleaseVersionState(current, string.IsNullOrWhiteSpace(previous) ? null : previous, updatedOn);
    }

    /// <summary>
    /// The releases an update from <paramref name="previousVersion"/> brought: newer than it, up to and including the
    /// current one, oldest first. Empty when there is no previous release or it cannot be read.
    /// </summary>
    public IReadOnlyList<HCMv3Release> ReleasesSince(string previousVersion)
    {
        if (!Version.TryParse(previousVersion, out var previous))
        {
            return Array.Empty<HCMv3Release>();
        }

        return _catalog.Releases.Where(r => r.Number > previous).ToList();
    }
}
