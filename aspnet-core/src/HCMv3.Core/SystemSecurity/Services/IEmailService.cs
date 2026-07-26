using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Templated-email sending engine (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.EmailService</c>).
///
/// ARCHITECTURE ADAPTATIONS (apply to every member below):
/// <list type="bullet">
/// <item><description>SMTP transport is delegated to ABP's own <c>Abp.Net.Mail.IEmailSender</c> (configured
/// via this app's tenant/host Email Settings), matching how <c>HCMv3.Authorization.Users.UserEmailer</c> and
/// <c>HCMv3.Notifications.EmailRealTimeNotifier</c> already send mail in this codebase. The legacy per-
/// template <c>EmailTemplate.EmailSetup</c> (SMTP host/port/credentials/office365-protocol-tweak) is
/// therefore unused -- <see cref="EmailTemplate.SmtpProfile"/> remains a free-text label only, per that
/// entity's own doc remarks.</description></item>
/// <item><description>Legacy's near-duplicate <c>SendEmail</c>/<c>SendEmail1</c> (and their
/// <c>parseTemplate</c>/<c>parseTemplate1</c> merge-token helpers, which differed only in incidental bugs --
/// e.g. <c>parseTemplate</c> forgot to filter <c>EmailToken</c>s by the given template) are consolidated into
/// a single faithful implementation per member here.</description></item>
/// <item><description>The legacy merge-field placeholder syntax is <c>$#TokenName#$</c> (verified against
/// source), not <c>{TokenName}</c> -- preserved as-is.</description></item>
/// <item><description>Every send attempt (successful or not) is persisted to
/// <see cref="EmailNotificationLog"/>, per this wave's dispatch instructions.</description></item>
/// <item><description>Legacy's <c>FirmLogo</c> inline-image (<c>LinkedResource</c>) embedding is dropped:
/// it depended on a <c>Company.Logo</c> byte[] field that has no equivalent on this port's Company-adjacent
/// entities (Wave B, not yet ported) -- see <c>Employee</c>'s own doc remarks about
/// <c>CurrentCompany</c>/etc. being Wave B.</description></item>
/// <item><description>Legacy's News/Announcement email members (<c>SendEmail_ForAnnouncement</c>,
/// <c>SendEmail(News, ...)</c>) are SKIPPED entirely: no <c>News</c>/announcement entity exists anywhere in
/// this port (Recruitment/GeneralMasterInfo modules were grepped -- no match).</description></item>
/// </list>
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends <paramref name="emailTemplate"/> to <paramref name="recipient"/>'s <see cref="Employee.Email"/>,
    /// merge-substituting its Subject/EmailBodyHtml/EmailBodyText via <see cref="ParseTemplateAsync"/>.
    /// Consolidates legacy <c>SendEmail</c>/<c>SendEmail1</c>/the <c>Attachment</c> overload/
    /// <c>SendEmailWithResponse</c> into one member (always returns a response instead of only sometimes
    /// throwing). No-ops (returns a failure response, no log row) if the recipient or its email is missing.
    /// </summary>
    Task<EmailResponseViewModel> SendEmailAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        object obj,
        IDictionary<string, object> mergeValues = null,
        Attachment attachment = null);

    /// <summary>
    /// Sends <paramref name="emailTemplate"/> directly to a raw email address (no <see cref="Employee"/>
    /// recipient). Ported from legacy <c>SendEmail_ForRecruiting</c>.
    /// </summary>
    Task<EmailResponseViewModel> SendEmailToAddressAsync(
        EmailTemplate emailTemplate,
        string emailAddress,
        object obj,
        IDictionary<string, object> mergeValues = null);

    /// <summary>
    /// Merge-field substitution ported from legacy <c>parseTemplate</c>/<c>parseTemplate1</c>: replaces every
    /// <c>$#TokenName#$</c> placeholder found via the <see cref="EmailToken"/> rows belonging to
    /// <paramref name="emailTemplate"/>. For each token: if <see cref="EmailToken.ObjectKey"/> is set, the
    /// value is looked up from <paramref name="mergeValues"/> by <see cref="EmailToken.TokenValue"/> key;
    /// otherwise it is read off <paramref name="obj"/> via <see cref="GetPropertyValue"/> using
    /// <see cref="EmailToken.TokenValue"/> as the (possibly dotted/nested) property path.
    /// <see cref="EmailToken.IsDateTimeType"/> values are formatted using this tenant's configured
    /// date/time format (<c>ISysSettingsAccessor</c>), matching a token name containing "Time" to the time
    /// format and any other date-typed token to the date format.
    /// </summary>
    Task<string> ParseTemplateAsync(
        string content,
        EmailTemplate emailTemplate,
        object obj,
        IDictionary<string, object> mergeValues = null);

    /// <summary>
    /// Reflection-based property reader ported near-verbatim from legacy <c>GetPropertyValue</c>. Supports
    /// dotted nested paths (e.g. <c>"Employee.FirstNameEn"</c>). Returns <c>null</c> (never throws) if the
    /// property does not exist or cannot be read.
    /// </summary>
    object GetPropertyValue(object src, string propName);

    /// <summary>
    /// Resolves recipients from <paramref name="notificationConfig"/>'s routing flags
    /// (<c>ToEmployee</c>/<c>ToRequestCreator</c>/<c>ToManager</c>/<c>SendToAllApproved</c>/
    /// <c>SendToAll</c>) and sends <paramref name="emailTemplate"/> to each (de-duplicated by email address),
    /// including each recipient's active <see cref="HCMv3.PersonnelAdministration.EmployeeDelegation"/>
    /// delegate (both the delegate and the original recipient are emailed, matching legacy behavior).
    /// Ported from legacy <c>SendEmail(NotificationConfig, obj, session, objList)</c>.
    ///
    /// ADAPTATION: legacy resolved <c>notificationConfig.EmailTemplate</c> directly; this port's
    /// <see cref="NotificationConfig"/> does not carry that FK (dropped in the entity's own porting wave --
    /// see its doc remarks), so the template must be supplied explicitly by the caller.
    ///
    /// SKIPPED: legacy's per-<c>NotificationGroup</c> fan-out to <c>EmployeeWorkflowRole</c> members
    /// (optionally filtered by <c>WorkflowRoleOrg</c>/<c>WorkflowRoleGroup</c>) is not ported -- this port's
    /// <see cref="NotificationGroup"/> entity dropped the <c>WorkflowRole</c>/<c>WorkflowRoleGroup</c>/
    /// <c>OrganizationUnitType</c> FKs it would need (Workflow/Organization modules out of scope for that
    /// entity's own wave -- see its doc remarks), and no <c>EmployeeWorkflowRole</c> entity exists in this
    /// port to query membership from regardless.
    /// </summary>
    Task SendEmailForNotificationConfigAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        IDictionary<string, object> mergeValues = null);

    /// <summary>
    /// Overload of <see cref="SendEmailForNotificationConfigAsync(NotificationConfig, EmailTemplate, object, IDictionary{string, object})"/>
    /// that additionally sends to every active <see cref="Employee"/> whose <see cref="Employee.Code"/> is in
    /// <paramref name="employeeCodeList"/> (comma-separated), each including their active delegate. Ported
    /// from legacy <c>SendEmail(NotificationConfig, obj, session, userList, objList)</c>.
    /// </summary>
    Task SendEmailForNotificationConfigAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        string employeeCodeList,
        IDictionary<string, object> mergeValues = null);

    /// <summary>
    /// Sends a raw (non-templated) punch-notification body to <paramref name="recipient"/>, logging the
    /// attempt. Ported from legacy <c>SendPunchEmailWithResponse</c>. When <paramref name="forEmployee"/> is
    /// <c>true</c>, behaves like <see cref="SendEmailAsync"/> (subject/body merged from
    /// <paramref name="emailTemplate"/>); otherwise the subject is still merged but the body is
    /// <paramref name="emailBody"/> verbatim (matching legacy's two branches).
    /// </summary>
    Task<EmailResponseViewModel> SendPunchEmailWithResponseAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        string emailBody,
        bool forEmployee,
        object obj = null,
        IDictionary<string, object> mergeValues = null);

    /// <summary>
    /// Sends a raw punch-notification body to every recipient resolved from
    /// <paramref name="notificationConfig"/>'s routing flags, returning one <see cref="EmailNotificationLog"/>
    /// per attempt (already persisted). Ported from legacy <c>SendPunchEmail</c>.
    ///
    /// ADAPTATION: legacy only fanned punch notifications out via <c>NotificationConfig.NotificationGroup</c>
    /// -&gt; <c>EmployeeWorkflowRole</c> membership. Since that data model is not ported (see
    /// <see cref="SendEmailForNotificationConfigAsync(NotificationConfig, EmailTemplate, object, IDictionary{string, object})"/>
    /// remarks), this port instead reuses the same <c>ToEmployee</c>/<c>ToManager</c>/<c>ToRequestCreator</c>/
    /// <c>SendToAllApproved</c>/<c>SendToAll</c> routing flags used for regular templated notifications.
    /// </summary>
    Task<List<EmailNotificationLog>> SendPunchEmailAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        string emailBody);
}
