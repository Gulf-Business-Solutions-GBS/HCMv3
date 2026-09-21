using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.MailKit;
using Abp.Net.Mail.Smtp;
using Castle.Core.Logging;
using HCMv3.Configuration;

namespace HCMv3.Net.Emailing;

/// <summary>
/// The SMTP on/off switch from the Integration Center.
///
/// WHY A GATE AND NOT A CONFIGURATION WIPE. The obvious way to implement "turn e-mail off" is to clear
/// the tenant's SMTP settings and restore them when it is switched back on. That does not work here:
///
/// <list type="number">
/// <item><description>ABP resolves a setting tenant-first, then host. A blank tenant value therefore
/// inherits the host's mail server rather than being empty, so on a multi-tenant server mail would keep
/// going out while the card read "off".</description></item>
/// <item><description>Where the host has no settings either,
/// <see cref="SmtpEmailSenderConfiguration"/> reads through <c>GetNotEmptySettingValue</c>, which throws.
/// Every password reset and notification would fail with an exception instead of quietly not
/// sending.</description></item>
/// <item><description>Restoring on re-enable needs a copy of the settings kept somewhere else, which is a
/// second source of truth: edit SMTP on the tenant settings screen while the switch is off, and switching
/// it back on would overwrite that edit with the stale copy.</description></item>
/// </list>
///
/// So the settings are left untouched and the send is intercepted instead. The gate sits on the two
/// <see cref="MailMessage"/> overloads rather than the string ones because every other overload funnels
/// through them -- and because it has to run BEFORE <c>NormalizeMail</c>, which is where the throw in
/// point 2 would otherwise happen.
///
/// Its shape follows the existing debug-mode <c>NullEmailSender</c> swap in <c>HCMv3CoreModule</c>; this
/// one is driven by a per-tenant setting instead of a build flag.
/// </summary>
public class HCMv3GatedEmailSender : MailKitEmailSender
{
    private readonly ISettingManager _settingManager;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public HCMv3GatedEmailSender(
        ISmtpEmailSenderConfiguration configuration,
        IMailKitSmtpBuilder smtpBuilder,
        ISettingManager settingManager)
        : base(configuration, smtpBuilder)
    {
        _settingManager = settingManager;
    }

    public override async Task SendAsync(MailMessage mail, bool normalize = true)
    {
        if (!await IsEnabledAsync())
        {
            LogSuppressed(mail);
            return;
        }

        await base.SendAsync(mail, normalize);
    }

    public override void Send(MailMessage mail, bool normalize = true)
    {
        if (!IsEnabled())
        {
            LogSuppressed(mail);
            return;
        }

        base.Send(mail, normalize);
    }

    /// <summary>
    /// Resolves for whatever scope the caller is in -- the current tenant, or the host for background work
    /// outside any tenant. Defaults to on, so a deployment that has never touched the switch behaves
    /// exactly as it did before the switch existed.
    /// </summary>
    private async Task<bool> IsEnabledAsync()
    {
        return await _settingManager.GetSettingValueAsync<bool>(AppSettings.Email.IsOutboundEmailEnabled);
    }

    private bool IsEnabled()
    {
        return _settingManager.GetSettingValue<bool>(AppSettings.Email.IsOutboundEmailEnabled);
    }

    /// <summary>
    /// Logged rather than dropped in silence: when someone reports a missing notification, this line is
    /// what tells an administrator the switch is off rather than the mail server being broken.
    /// </summary>
    private void LogSuppressed(MailMessage mail)
    {
        Logger.Info(
            "Outbound e-mail is switched off in the Integration Center; not sending \"" +
            (mail?.Subject ?? "") + "\".");
    }
}
