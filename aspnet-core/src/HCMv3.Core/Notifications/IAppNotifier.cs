using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using Abp.Localization;
using Abp.Notifications;
using HCMv3.Authorization.Users;
using HCMv3.MultiTenancy;

namespace HCMv3.Notifications;

public interface IAppNotifier
{
    Task WelcomeToTheApplicationAsync(User user);

    Task NewUserRegisteredAsync(User user);

    Task NewTenantRegisteredAsync(Tenant tenant);

    Task GdprDataPrepared(UserIdentifier user, Guid binaryObjectId);

    Task SendMessageAsync(UserIdentifier user, string message, NotificationSeverity severity = NotificationSeverity.Info);

    /// <summary>
    /// A one-line message that also carries structured data for the client to act on.
    ///
    /// <para>
    /// The plain overload above puts everything in prose, which is fine for "your export is ready" and
    /// useless for anything the client must navigate to: a workflow notification said
    /// "EXP-2026-00003 has been sent to you for approval" and the request id existed nowhere except inside
    /// that sentence, so clicking it could not open the request. The user reported exactly that. Extra
    /// properties ride alongside the message on the same <c>MessageNotificationData</c>, which is how
    /// <see cref="NewUserRegisteredAsync"/> and the rest already attach their own.
    /// </para>
    /// </summary>
    Task SendMessageAsync(UserIdentifier user, string message, IDictionary<string, object> data,
        NotificationSeverity severity = NotificationSeverity.Info);

    Task SendMessageAsync(string notificationName, string message, UserIdentifier[] userIds = null,
        NotificationSeverity severity = NotificationSeverity.Info);

    Task SendMessageAsync(UserIdentifier user, LocalizableString localizableMessage, IDictionary<string, object> localizableMessageData = null, NotificationSeverity severity = NotificationSeverity.Info);

    Task TenantsMovedToEdition(UserIdentifier user, string sourceEditionName, string targetEditionName);

    Task SomeUsersCouldntBeImported(UserIdentifier user, string fileToken, string fileType, string fileName);

    Task SendMassNotificationAsync(string message, UserIdentifier[] userIds = null,
        NotificationSeverity severity = NotificationSeverity.Info, Type[] targetNotifiers = null);
}

