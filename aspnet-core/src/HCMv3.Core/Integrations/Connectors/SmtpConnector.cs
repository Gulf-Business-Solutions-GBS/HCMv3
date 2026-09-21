using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Net.Mail;
using Abp.Runtime.Session;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// Outbound e-mail over SMTP.
///
/// STORAGE: ABP's <c>SettingManager</c>, which is where ABP's own e-mail pipeline reads it from. Moving
/// these values into <see cref="IntegrationSetting"/> would mean overriding
/// <c>ISmtpEmailSenderConfiguration</c> to read from somewhere else and re-doing that work on every
/// framework upgrade, in exchange for nothing an administrator can see. The Integration Center writes
/// through to the same settings the existing tenant settings screen edits, so the two never disagree.
///
/// THE ON/OFF SWITCH is ours, not ABP's: it writes
/// <see cref="AppSettings.Email.IsOutboundEmailEnabled"/>, which <c>HCMv3GatedEmailSender</c> checks before
/// every send. The settings themselves are never cleared -- see that class for why clearing them would
/// fail to stop mail rather than succeed.
/// </summary>
public class SmtpConnector : HCMv3DomainServiceBase, IIntegrationConnector
{
    public const string FieldHost = "Host";
    public const string FieldPort = "Port";
    public const string FieldUserName = "UserName";
    public const string FieldPassword = "Password";
    public const string FieldDomain = "Domain";
    public const string FieldEnableSsl = "EnableSsl";
    public const string FieldDefaultFromAddress = "DefaultFromAddress";
    public const string FieldDefaultFromDisplayName = "DefaultFromDisplayName";

    private readonly ISettingManager _settingManager;
    private readonly IEmailSender _emailSender;
    private readonly IRepository<User, long> _userRepository;
    private readonly IAbpSession _abpSession;

    public SmtpConnector(
        ISettingManager settingManager,
        IEmailSender emailSender,
        IRepository<User, long> userRepository,
        IAbpSession abpSession)
    {
        _settingManager = settingManager;
        _emailSender = emailSender;
        _userRepository = userRepository;
        _abpSession = abpSession;
    }

    public string Key => IntegrationKeys.Smtp;

    public IntegrationCategory Category => IntegrationCategory.Communication;

    public string NameKey => "Integration_Smtp";

    public string DescriptionKey => "Integration_Smtp_Description";

    public bool SupportsTest => true;

    public string ExclusiveGroup => null;

    public bool SupportsEnableToggle => true;

    public string ExternalManageRoute => "/app/admin/settings";

    public IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>
    {
        new(FieldHost, "Integration_Smtp_Host", IntegrationFieldType.Text, true, false),
        new(FieldPort, "Integration_Smtp_Port", IntegrationFieldType.Number, true, false, null, "587"),
        new(FieldUserName, "Integration_Smtp_UserName", IntegrationFieldType.Text, false, false),
        new(FieldPassword, "Integration_Smtp_Password", IntegrationFieldType.Password, false, true),
        new(FieldDomain, "Integration_Smtp_Domain", IntegrationFieldType.Text, false, false),
        new(FieldEnableSsl, "Integration_Smtp_EnableSsl", IntegrationFieldType.Boolean, false, false, null, "true"),
        new(FieldDefaultFromAddress, "Integration_Smtp_DefaultFromAddress", IntegrationFieldType.Email, true, false),
        new(FieldDefaultFromDisplayName, "Integration_Smtp_DefaultFromDisplayName", IntegrationFieldType.Text, true, false)
    };

    public async Task<IntegrationConnectorState> GetStateAsync()
    {
        var host = await GetSettingAsync(EmailSettingNames.Smtp.Host);
        var fromAddress = await GetSettingAsync(EmailSettingNames.DefaultFromAddress);

        var isConfigured = !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(fromAddress);

        var isOutboundEnabled = await _settingManager.GetSettingValueForTenantAsync<bool>(
            AppSettings.Email.IsOutboundEmailEnabled,
            _abpSession.GetTenantId());

        return new IntegrationConnectorState
        {
            IsConfigured = isConfigured,
            IsEnabled = isConfigured && isOutboundEnabled
        };
    }

    public async Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        var password = await GetSettingAsync(EmailSettingNames.Smtp.Password);

        return new List<IntegrationFieldValue>
        {
            Value(FieldHost, await GetSettingAsync(EmailSettingNames.Smtp.Host)),
            Value(FieldPort, await GetSettingAsync(EmailSettingNames.Smtp.Port)),
            Value(FieldUserName, await GetSettingAsync(EmailSettingNames.Smtp.UserName)),
            new(FieldPassword, null, !string.IsNullOrWhiteSpace(password)),
            Value(FieldDomain, await GetSettingAsync(EmailSettingNames.Smtp.Domain)),
            Value(FieldEnableSsl, await GetSettingAsync(EmailSettingNames.Smtp.EnableSsl)),
            Value(FieldDefaultFromAddress, await GetSettingAsync(EmailSettingNames.DefaultFromAddress)),
            Value(FieldDefaultFromDisplayName, await GetSettingAsync(EmailSettingNames.DefaultFromDisplayName))
        };
    }

    public async Task SaveValuesAsync(IReadOnlyDictionary<string, string> values)
    {
        string Submitted(string key) => values != null && values.TryGetValue(key, out var v) ? v : null;

        await SetSettingAsync(EmailSettingNames.Smtp.Host, Submitted(FieldHost));
        await SetSettingAsync(EmailSettingNames.Smtp.Port, Submitted(FieldPort));
        await SetSettingAsync(EmailSettingNames.Smtp.UserName, Submitted(FieldUserName));
        await SetSettingAsync(EmailSettingNames.Smtp.Domain, Submitted(FieldDomain));
        await SetSettingAsync(EmailSettingNames.Smtp.EnableSsl, Submitted(FieldEnableSsl));
        await SetSettingAsync(EmailSettingNames.DefaultFromAddress, Submitted(FieldDefaultFromAddress));
        await SetSettingAsync(EmailSettingNames.DefaultFromDisplayName, Submitted(FieldDefaultFromDisplayName));

        // Authentication is used whenever a user name is supplied -- the same rule the tenant settings
        // screen applies, kept here so the two editing paths cannot disagree.
        var userName = Submitted(FieldUserName);
        await SetSettingAsync(
            EmailSettingNames.Smtp.UseDefaultCredentials,
            string.IsNullOrWhiteSpace(userName).ToString(CultureInfo.InvariantCulture));

        var password = Submitted(FieldPassword);
        if (!string.IsNullOrWhiteSpace(password))
        {
            // Blank means "keep the stored password". ABP stores this one encrypted, as the tenant
            // settings screen does.
            await SetSettingAsync(EmailSettingNames.Smtp.Password, IntegrationSecret.Protect(password));
        }
    }

    /// <summary>
    /// Sends a real message to the signed-in administrator, because an SMTP configuration that connects
    /// but cannot deliver is not a working configuration.
    /// </summary>
    public async Task<IntegrationTestResult> TestConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // The sender this test goes through is the gated one, which drops the message when the switch
            // is off. Without this guard the test would report success having sent nothing at all.
            var isOutboundEnabled = await _settingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.Email.IsOutboundEmailEnabled,
                _abpSession.GetTenantId());

            if (!isOutboundEnabled)
            {
                return Fail(stopwatch, L("Integration_Smtp_DisabledCannotTest"));
            }

            var userId = _abpSession.UserId;
            if (userId == null)
            {
                return Fail(stopwatch, L("IntegrationTestNeedsSignedInUser"));
            }

            var user = await _userRepository.FirstOrDefaultAsync(userId.Value);
            if (user == null || string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                return Fail(stopwatch, L("IntegrationTestNeedsSignedInUser"));
            }

            await _emailSender.SendAsync(user.EmailAddress, L("TestEmail_Subject"), L("TestEmail_Body"));

            stopwatch.Stop();

            return new IntegrationTestResult
            {
                Succeeded = true,
                Message = L("Integration_Smtp_TestEmailSent", user.EmailAddress),
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return Fail(stopwatch, ex.Message);
        }
    }

    /// <summary>
    /// Switches outbound e-mail on or off for this tenant. Nothing is cleared: only the gate moves, so
    /// switching back on needs no stored copy of the server settings and cannot overwrite an edit made
    /// meanwhile on the tenant settings screen.
    ///
    /// Turning it off stops password-reset messages too, which is why the card confirms before switching
    /// off rather than flipping on one click.
    /// </summary>
    public async Task SetEnabledAsync(bool enabled)
    {
        await _settingManager.ChangeSettingForTenantAsync(
            _abpSession.GetTenantId(),
            AppSettings.Email.IsOutboundEmailEnabled,
            enabled.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<string> GetSettingAsync(string name)
    {
        var tenantId = _abpSession.GetTenantId();
        var value = await _settingManager.GetSettingValueForTenantAsync(name, tenantId);

        return string.Equals(name, EmailSettingNames.Smtp.Password, StringComparison.Ordinal)
            ? IntegrationSecret.Reveal(value)
            : value;
    }

    private async Task SetSettingAsync(string name, string value)
    {
        await _settingManager.ChangeSettingForTenantAsync(_abpSession.GetTenantId(), name, value);
    }

    private static IntegrationFieldValue Value(string key, string value)
    {
        return new IntegrationFieldValue(key, value, !string.IsNullOrWhiteSpace(value));
    }

    private static IntegrationTestResult Fail(Stopwatch stopwatch, string message)
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }

        return new IntegrationTestResult
        {
            Succeeded = false,
            Message = message,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
    }
}
