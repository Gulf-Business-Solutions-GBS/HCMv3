using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Net.Mail;
using HCMv3.Debugging;

namespace HCMv3.Net.Emailing;

public class EmailSettingsChecker : IEmailSettingsChecker, ITransientDependency
{
    /// <summary>
    /// The placeholder values ASP.NET Zero seeds. They are present and non-empty, so an emptiness
    /// check alone would accept them and hand MailKit a hostname that does not exist.
    /// </summary>
    public const string PlaceholderFromAddress = "admin@mydomain.com";

    public const string PlaceholderHost = "mydomain.com mailer";

    private readonly ISettingManager _settingManager;

    public EmailSettingsChecker(ISettingManager settingManager)
    {
        _settingManager = settingManager;
    }

    /// <summary>
    /// Names the settings that stop email being sent, rather than reporting a single "not provided".
    /// Kept free of ISettingManager and DebugHelper so the rules can be tested directly: the public
    /// entry points below short-circuit under a debug build, which would otherwise make any test of
    /// them pass without exercising a single rule.
    /// </summary>
    /// <returns>Localization keys, in the order the settings appear on the settings screen.</returns>
    public static IReadOnlyList<string> FindMissingSettings(
        string defaultFromAddress,
        string host,
        bool useDefaultCredentials,
        string userName,
        string password)
    {
        var missing = new List<string>();

        if (defaultFromAddress.IsNullOrEmpty() || defaultFromAddress == PlaceholderFromAddress)
        {
            missing.Add("DefaultFromAddress");
        }

        if (host.IsNullOrEmpty() || host == PlaceholderHost)
        {
            missing.Add("SmtpHost");
        }

        // Windows credentials are taken from the account the host runs as, so there is nothing to
        // enter. Only when authentication is switched on does a user name and password apply.
        if (useDefaultCredentials)
        {
            return missing;
        }

        if (userName.IsNullOrEmpty())
        {
            missing.Add("UserName");
        }

        if (password.IsNullOrEmpty())
        {
            missing.Add("Password");
        }

        return missing;
    }

    public bool EmailSettingsValid()
    {
        return GetMissingEmailSettingKeys().Count == 0;
    }

    public async Task<bool> EmailSettingsValidAsync()
    {
        return (await GetMissingEmailSettingKeysAsync()).Count == 0;
    }

    public IReadOnlyList<string> GetMissingEmailSettingKeys()
    {
        if (DebugHelper.IsDebug)
        {
            return new List<string>();
        }

        return FindMissingSettings(
            _settingManager.GetSettingValue(EmailSettingNames.DefaultFromAddress),
            _settingManager.GetSettingValue(EmailSettingNames.Smtp.Host),
            _settingManager.GetSettingValue<bool>(EmailSettingNames.Smtp.UseDefaultCredentials),
            _settingManager.GetSettingValue(EmailSettingNames.Smtp.UserName),
            _settingManager.GetSettingValue(EmailSettingNames.Smtp.Password)
        );
    }

    public async Task<IReadOnlyList<string>> GetMissingEmailSettingKeysAsync()
    {
        if (DebugHelper.IsDebug)
        {
            return new List<string>();
        }

        return FindMissingSettings(
            await _settingManager.GetSettingValueAsync(EmailSettingNames.DefaultFromAddress),
            await _settingManager.GetSettingValueAsync(EmailSettingNames.Smtp.Host),
            await _settingManager.GetSettingValueAsync<bool>(EmailSettingNames.Smtp.UseDefaultCredentials),
            await _settingManager.GetSettingValueAsync(EmailSettingNames.Smtp.UserName),
            await _settingManager.GetSettingValueAsync(EmailSettingNames.Smtp.Password)
        );
    }
}
