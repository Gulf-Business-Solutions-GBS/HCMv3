using System.Collections.Generic;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Works out WHO a <see cref="NotificationConfig"/> is addressed to.
///
/// <para>
/// Split out of <see cref="IEmailService"/> on 2026-09-22, when in-app notifications joined email as a second
/// channel. The routing flags on a configuration say who should be told, not how; if each channel answered
/// that question for itself the two would drift, and an approver would be told in the app about a request
/// nobody emailed them about — or the reverse, which is worse, because the email is the one people act on.
/// </para>
///
/// <para>
/// Ported from legacy <c>EmailService.SendEmail(NotificationConfig, ...)</c>, which resolved recipients inline
/// as it sent. The one deliberate difference is that legacy's per-<c>NotificationGroup</c> fan-out to workflow
/// role holders, skipped in the first pass of this port for want of the entities it needed, is included here.
/// </para>
/// </summary>
public interface INotificationRecipientResolver
{
    /// <summary>
    /// Everyone <paramref name="notificationConfig"/> addresses about <paramref name="obj"/>, de-duplicated and
    /// with inactive employees dropped. Each recipient's stand-in, if they have cover in place today, is
    /// included alongside them — not instead of them, matching legacy.
    ///
    /// <para>
    /// <paramref name="obj"/> is whatever the notification is about: an <see cref="Employee"/>, or anything
    /// carrying an <c>EmployeeId</c> (a <c>WorkflowRequest</c>, a leave row) from which the subject is read.
    /// Flags that need a workflow request — the approver ones — simply find nobody for anything else.
    /// </para>
    /// </summary>
    /// <param name="employeeCodeList">Extra recipients by employee code, comma-separated. Legacy's
    /// <c>userList</c> parameter.</param>
    List<Employee> Resolve(NotificationConfig notificationConfig, object obj, string employeeCodeList = null);
}
