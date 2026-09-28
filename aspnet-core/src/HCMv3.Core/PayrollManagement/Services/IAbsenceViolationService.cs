using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The absence-violation escalation job, ported from legacy
/// <c>NotificationService.SendNotification_AbsentViolation</c> and
/// <c>NotificationService.SendNotification_NotReturnViolation</c>
/// (<c>HCM10.Module/BusinessObjects/Services/NotificationService.cs</c> lines 2623-3269). Legacy ran both once a
/// day from the <c>SendRealTimeNotification</c> Hangfire job; here <c>DailyNotificationWorker</c> calls them for
/// each tenant.
///
/// <para>
/// Both walk an <see cref="AbsenceViolationRule"/>'s steps (<see cref="AbsenceViolationRuleDetail"/>) in order.
/// Each step can file a disciplinary record, book a leave, or only notify, and every step taken is written to
/// <see cref="AbsenceViolationActionLog"/> under one <see cref="AbsenceViolationActionRecord"/> per absent day or
/// per leave. That log is what makes a re-run safe: a step already logged is never taken again, and a record
/// is closed once its last step has fired or the day turned out to be excused.
/// </para>
///
/// <para>
/// Both work on the current tenant. <paramref name="today"/> is passed in rather than read from the clock so
/// the day boundaries are the caller's, and testable.
/// </para>
/// </summary>
public interface IAbsenceViolationService : IDomainService
{
    /// <summary>
    /// Legacy <c>SendNotification_AbsentViolation</c>: working days with no punch in and no punch out, up to
    /// yesterday, for active employees, under the rule set in time settings
    /// (<c>ManageTimeSettings.AbsenceViolationRuleId</c>, from <c>AbsenceViolationEffectiveDate</c> when set).
    /// A day turns out excused when the employee punched, asked for a missing punch, or has a leave covering it
    /// that was not rejected; its record is then closed and nothing is sent.
    /// </summary>
    Task SendAbsentViolationNotificationsAsync(DateTime today);

    /// <summary>
    /// Legacy <c>SendNotification_NotReturnViolation</c>: leave of a type that carries its own rule
    /// (<c>AbsenceType.AbsenceViolationRuleId</c>) that ended before today with no return recorded. At most one
    /// step per leave per run, once the step's day interval after the leave's end date has passed.
    /// </summary>
    Task SendNotReturnViolationNotificationsAsync(DateTime today);
}
