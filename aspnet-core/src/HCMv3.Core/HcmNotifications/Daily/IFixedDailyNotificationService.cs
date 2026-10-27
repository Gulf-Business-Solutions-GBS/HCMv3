using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.HcmNotifications.Daily;

/// <summary>
/// The six fixed notifications legacy <c>SendRealTimeNotification</c> ran every day that were still not
/// running in HCMv3: employee and company document expiry, probation end, separation, annual leave balance
/// over the limit, and news. Ported AS-IS, legacy oddities included, each pinned by a test -- they are not
/// rewritten as Notification Master rules, because customers rely on exactly what they do.
///
/// <para>
/// Each returns how many notices it sent (for tests and the run log). Each reads "now" from its argument, not
/// the clock, so a run that crosses midnight does not change its mind halfway.
/// </para>
///
/// <para>
/// ONE DELIBERATE ADDITION: legacy ran once a day; the HCMv3 worker runs every four hours. The two notices
/// legacy keyed only on "today is the day" (probation, separation) would otherwise go out up to six times on
/// that day, so each send is logged in <see cref="NotificationSentLog"/> and repeated at most once per record
/// per day. The other four need nothing extra: the three legacy kept a flag for (company document, annual
/// leave quota, news) still set that flag, and employee document expiry's own frequency rule already refuses a
/// second send the same day.
/// </para>
/// </summary>
public interface IFixedDailyNotificationService : IDomainService
{
    /// <summary>
    /// Legacy "Employee Document Expiry Notification": every document of an active employee whose expiry date
    /// minus its type's notification period has been reached, through the type's Notification Config, repeated
    /// by the type's frequency (once / daily / weekly / monthly) up to its count limit.
    /// </summary>
    Task<int> SendEmployeeDocumentExpiryNotificationsAsync(DateTime now);

    /// <summary>
    /// Legacy "Organization Document Expiry Notification": every company document whose expiry date minus its
    /// type's notification period has been reached and whose <c>NotificationSent</c> is still false, once.
    /// </summary>
    Task<int> SendCompanyDocumentExpiryNotificationsAsync(DateTime now);

    /// <summary>
    /// Legacy <c>SendNotification_Probation</c> (<c>PROBATION_NOTIFICATION</c>): active employees whose
    /// probation ends exactly <c>ReminderDays</c> from today.
    /// </summary>
    Task<int> SendProbationNotificationsAsync(DateTime now);

    /// <summary>
    /// Legacy <c>SendNotification_Separation</c> (<c>Separation_NOTIFICATION</c>): separations whose last day in
    /// service is exactly <c>ReminderDays</c> from today.
    /// </summary>
    Task<int> SendSeparationNotificationsAsync(DateTime now);

    /// <summary>
    /// Legacy "Employee AnnualLeaveMaxLimit Notification" (<c>ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION</c>):
    /// active employees whose current annual leave (absence type code "1") quota balance has reached their
    /// sub group's "max limit reach notification" days, once per quota.
    /// </summary>
    Task<int> SendAnnualLeaveMaxLimitNotificationsAsync(DateTime now);

    /// <summary>
    /// Legacy "News/Announcement Notification": news in its issue-to-expiry window and not yet sent, in-app and/or
    /// by email to the employees of its organisation unit, once.
    /// </summary>
    Task<int> SendNewsNotificationsAsync(DateTime now);
}
