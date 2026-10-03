using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.Configuration;

namespace HCMv3.SetupChecks;

/// <summary>Which build a tenant is on, and which it was on before the latest update.</summary>
public record ReleaseVersionState(string CurrentVersion, string PreviousVersion, DateTime? UpdatedOn)
{
    /// <summary>False until an update has happened since tracking began: there is nothing to compare with.</summary>
    public bool HasPrevious => !string.IsNullOrEmpty(PreviousVersion);
}

/// <summary>
/// Remembers, per tenant, the last HCMv3 build it saw, so an update can be told apart from an ordinary visit.
///
/// <para>Kept in three tenant settings (<see cref="AppSettings.Releases"/>). The first time a tenant is seen the
/// current version is recorded and there is nothing to compare. When the running build's version later differs from
/// the one recorded, the recorded one becomes the PREVIOUS version and the current one is recorded -- so "previous to
/// current" stays on screen until the next update, however many times the page is opened.</para>
/// </summary>
public class ReleaseVersionTracker : HCMv3DomainServiceBase
{
    private readonly IReleaseLog _log;

    public ReleaseVersionTracker(IReleaseLog log)
    {
        _log = log;
    }

    public async Task<ReleaseVersionState> RecordAsync(int tenantId)
    {
        var current = _log.Version;
        var lastSeen = await SettingManager.GetSettingValueForTenantAsync(AppSettings.Releases.LastSeenVersion, tenantId);

        if (string.IsNullOrWhiteSpace(lastSeen))
        {
            // First sight: record where tracking starts; no previous version to compare with.
            await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.LastSeenVersion, current);
        }
        else if (!string.Equals(lastSeen.Trim(), current, StringComparison.Ordinal))
        {
            // The build moved on since the tenant was last seen: that is an update.
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
}
