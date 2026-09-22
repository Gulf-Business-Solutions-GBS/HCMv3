using System.Threading.Tasks;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Scheduled-notification dispatch engine (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.NotificationService</c>). Named
/// <c>NotificationDispatchService</c> (not <c>NotificationService</c>) to avoid a naming collision with
/// ABP's own <c>Abp.Notifications</c> namespace concepts already used elsewhere in this codebase (e.g.
/// <c>HCMv3.Notifications.AppNotificationProvider</c>).
///
/// ARCHITECTURE ADAPTATIONS / SCOPE (apply across this whole port):
/// <list type="bullet">
/// <item><description>Legacy methods took an XAF <c>IObjectSpace</c> parameter for data access; this port's
/// methods take none (ABP repositories are constructor-injected instead), per this wave's dispatch
/// instructions.</description></item>
/// <item><description>Every concrete method resolves its routing config via the already-ported
/// <c>SystemNotification</c> entity (matched by <c>NotificationName</c>, mirroring legacy's hardcoded
/// constant string per notification type) and its linked <c>NotificationConfig</c>.</description></item>
/// <item><description>ADAPTATION: legacy resolved the <c>EmailTemplate</c> to send straight off
/// <c>NotificationConfig.EmailTemplate</c>; since that FK was not ported onto this port's
/// <c>NotificationConfig</c> entity (see its own doc remarks), each method here instead resolves the
/// <c>EmailTemplate</c> by convention: <c>EmailTemplate.Code == SystemNotification.NotificationName</c>
/// (e.g. an admin creates an <c>EmailTemplate</c> with Code <c>"BIRTHDAY_NOTIFICATION"</c>). If no such
/// template exists, the email leg of that notification is skipped (no-op), while any equivalent in-app
/// notification would still (conceptually) apply.</description></item>
/// <item><description>ADAPTATION: legacy's "in-app" notification leg (<c>NotificationConfig.SendNotification</c>
/// == true) created a <c>TaskEvent</c>/<c>EmployeesNotification</c> row -- a legacy to-do/reminder concept
/// with no equivalent entity ported anywhere in this codebase (superseded in spirit by ABP's own
/// <c>Abp.Notifications</c> infrastructure, e.g. <c>HCMv3.Notifications.AppNotificationProvider</c> +
/// <c>Abp.Notifications.INotificationPublisher</c>). Wiring a full replacement (defining
/// <c>NotificationDefinition</c>s, publishing via <c>INotificationPublisher</c>, mapping Employee -&gt; User)
/// is out of scope for this wave; the <c>SendNotification</c> flag is therefore currently a no-op here
/// (only the <c>SendEmail</c> flag has an effect) -- documented, not silently dropped.</description></item>
/// </list>
///
/// DEFERRED / SKIPPED members (documented per dispatch instructions, not ported):
/// <list type="bullet">
/// <item><description><c>GetMobileAppNotificationContent</c> (both overloads) -- mobile-app-specific
/// response-shaping helper, explicitly out of scope per dispatch instructions.</description></item>
/// <item><description><c>SendRealTimeNotification</c> -- the legacy method is a ~900-line scheduler
/// covering many unrelated notification sources (document expiry, punch alerts, workflow reminders, etc.),
/// most of which depend on entities not ported in this or prior waves (e.g. <c>EmployeeDocument
/// .NotificationSent</c>/<c>TaskEvent</c>/<c>EmployeesNotification</c>). Porting it in full is a dedicated
/// future background-job wave; this service exposes the two narrowly-scoped, self-contained pieces that
/// *are* portable today (birthday/anniversary) instead of the monolithic scheduler.</description></item>
/// <item><description><c>UpdateEmployeeWorkFlowRoleList</c> -- depends on the legacy
/// <c>EmployeeWorkflowRole</c> membership concept, which (per this wave's own <c>NotificationGroup</c>/
/// <c>IEmailService</c> remarks) has no equivalent entity in this port.</description></item>
/// </list>
/// </summary>
public interface INotificationDispatchService
{
    /// <summary>
    /// Sends birthday notifications to every active <see cref="HCMv3.PersonnelAdministration.Employee"/>
    /// whose <c>BirthDate</c> month/day matches today, per the <c>"BIRTHDAY_NOTIFICATION"</c>
    /// <see cref="HCMv3.SystemSecurity.SystemNotification"/> row's linked <c>NotificationConfig</c>. Ported
    /// from legacy <c>SendNotification_BirthDay</c>. No-ops if no matching active
    /// <c>SystemNotification</c>/<c>NotificationConfig</c>/<c>EmailTemplate</c> is configured.
    /// </summary>
    Task SendBirthdayNotificationsAsync();

    /// <summary>
    /// Sends work-anniversary notifications to every active Employee whose
    /// <c>JoiningDate.AddYears(yearsOfService)</c> falls on today, per the
    /// <c>"ANNIVERSARY_NOTIFICATION"</c> <see cref="HCMv3.SystemSecurity.SystemNotification"/> row's linked
    /// <c>NotificationConfig</c>. Ported from legacy <c>SendAnniversaryEmails</c> +
    /// <c>CalculateYearsOfService</c> (years-of-service = current year minus joining year, matching legacy's
    /// exact -- deliberately simplistic -- calculation).
    /// </summary>
    Task SendAnniversaryNotificationsAsync();

    /// <summary>
    /// Sends advance-leave-payment reminder notifications, ported from legacy
    /// <c>SendNotification_AdvancePayment</c>, per the <c>"AdvancePayment_NOTIFICATION"</c>
    /// <see cref="HCMv3.SystemSecurity.SystemNotification"/> row's linked <c>NotificationConfig</c> and
    /// <see cref="HCMv3.SystemSecurity.SystemNotification.ReminderDays"/>. Matches active
    /// <see cref="HCMv3.PersonnelAdministration.EmployeeAbsence"/> rows with a paid
    /// <see cref="HCMv3.PayrollManagement.AbsenceType"/>,
    /// <c>LeavePaymentType == LeavePaymentType.PayInAdvance</c> and <c>AdvanceLeavePaid == false</c>, whose
    /// reminder date (either <c>PaymentDate</c> or <c>StartDate</c>, chosen per the tenant's
    /// <see cref="HCMv3.SystemSettings.ManageNotificationSettings.AdvancePaymentReminderDateType"/>) minus
    /// <c>ReminderDays</c> equals today.
    /// </summary>
    Task SendAdvancePaymentNotificationsAsync();

    /// <summary>
    /// Sends leave-resumption reminder notifications, ported from legacy
    /// <c>SendNotification_LeaveResumption</c>, per the <c>"LEAVERESUMPTION_NOTIFICATION"</c>
    /// <see cref="HCMv3.SystemSecurity.SystemNotification"/> row's linked <c>NotificationConfig</c>. Matches
    /// active <see cref="HCMv3.PersonnelAdministration.EmployeeAbsence"/> rows whose
    /// <see cref="HCMv3.PayrollManagement.AbsenceType"/> requires a return date, that have not yet been
    /// returned (<c>IsReturned == false</c>, <c>ReturnDate</c> unset) and whose <c>EndDate</c> has already
    /// passed.
    /// ADAPTATION: legacy additionally cross-referenced the derived <c>AbsenceRequest</c>/
    /// <c>WorkflowRequest</c> workflow entities to exclude employees whose return was already recorded via
    /// an approved workflow request; that Workflow-module cross-check is not applicable here because
    /// <see cref="HCMv3.PersonnelAdministration.EmployeeAbsence"/>'s <c>WfRequest</c> FK was deliberately not
    /// ported (see that entity's own remarks) -- <c>IsReturned</c>/<c>ReturnDate</c> on
    /// <see cref="HCMv3.PersonnelAdministration.EmployeeAbsence"/> itself is the sole source of truth in this
    /// port.
    /// </summary>
    Task SendLeaveResumptionNotificationsAsync();

    /// <summary>
    /// Tells the guarantor that an employee has named them on a passport withdrawal request (legacy
    /// <c>SendEmailToGuarantor</c>, notification <c>PassportGuarantor_Notification</c>). A no-op when the
    /// tenant has not configured that notification, has e-mail switched off for it, or either person is no
    /// longer active -- the request itself is never held up by it.
    /// </summary>
    Task SendPassportGuarantorNotificationAsync(int guarantorId, int employeeId, string requestNumber);

    /// <summary>
    /// Sends punch-during-leave notifications, ported from legacy
    /// <c>SendNotification_PunchDuringLeave</c>, per the <c>"PUNCHDURINGLEAVE_NOTIFICATION"</c>
    /// <see cref="HCMv3.SystemSecurity.SystemNotification"/> row's linked <c>NotificationConfig</c>. Matches
    /// active, not-yet-returned <see cref="HCMv3.PersonnelAdministration.EmployeeAbsence"/> rows (return-date
    /// required, currently within/past their leave range) that have at least one
    /// <see cref="HCMv3.TimeManagement.TimeEvent"/> punch recorded strictly within the leave's
    /// <c>(StartDate, EndDate]</c> window.
    /// </summary>
    Task SendPunchDuringLeaveNotificationsAsync();
}
