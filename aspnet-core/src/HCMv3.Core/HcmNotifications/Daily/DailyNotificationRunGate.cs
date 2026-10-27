using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;

namespace HCMv3.HcmNotifications.Daily;

/// <summary>
/// Keeps the daily notification run to once per tenant per calendar day, as legacy's once-a-day job did.
/// </summary>
public interface IDailyNotificationRunGate
{
    /// <summary>True when the daily run has already completed for <paramref name="tenantId"/> on <paramref name="day"/>.</summary>
    Task<bool> HasRunAsync(int tenantId, DateTime day);

    /// <summary>Records that the daily run completed for <paramref name="tenantId"/> on <paramref name="day"/>.</summary>
    Task MarkRunAsync(int tenantId, DateTime day);
}

/// <inheritdoc cref="IDailyNotificationRunGate"/>
/// <remarks>
/// Stored as an ABP tenant setting (<see cref="HcmNotificationSettingNames.DailyRunCompletedOn"/>) rather than a
/// table of its own: one value per tenant, the same bookkeeping pattern as the subscription hand-out.
/// </remarks>
public class DailyNotificationRunGate : IDailyNotificationRunGate, ITransientDependency
{
    private const string DayFormat = "yyyy-MM-dd";

    private readonly ISettingManager _settingManager;

    public DailyNotificationRunGate(ISettingManager settingManager)
    {
        _settingManager = settingManager;
    }

    public async Task<bool> HasRunAsync(int tenantId, DateTime day)
    {
        var value = await _settingManager.GetSettingValueForTenantAsync(HcmNotificationSettingNames.DailyRunCompletedOn, tenantId);
        return value == Format(day);
    }

    public Task MarkRunAsync(int tenantId, DateTime day)
    {
        return _settingManager.ChangeSettingForTenantAsync(tenantId, HcmNotificationSettingNames.DailyRunCompletedOn, Format(day));
    }

    private static string Format(DateTime day) => day.Date.ToString(DayFormat, CultureInfo.InvariantCulture);
}
