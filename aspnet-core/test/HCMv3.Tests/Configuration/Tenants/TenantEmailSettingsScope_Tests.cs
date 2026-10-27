using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Net.Mail;
using Abp.Net.Mail.Smtp;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Configuration.Tenants;

/// <summary>
/// Item 298: tenants may keep their own SMTP settings. Switched on by ASP.NET Zero's supported switch,
/// <c>HCMv3Consts.AllowTenantsToChangeEmailSettings = true</c>, which stops <c>AppSettingProvider</c> narrowing
/// the SMTP setting definitions to the host. The sender then reads tenant first and falls back to the host.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TenantEmailSettingsScope_Tests : AppTestBase
{
    [Fact]
    public void Should_Let_Tenants_Hold_Their_Own_Smtp_Settings()
    {
        HCMv3Consts.AllowTenantsToChangeEmailSettings.ShouldBeTrue();

        var definitions = Resolve<ISettingDefinitionManager>();

        foreach (var name in new[]
                 {
                     EmailSettingNames.Smtp.Host, EmailSettingNames.Smtp.Port, EmailSettingNames.Smtp.UserName,
                     EmailSettingNames.Smtp.Password, EmailSettingNames.Smtp.EnableSsl,
                     EmailSettingNames.DefaultFromAddress, EmailSettingNames.DefaultFromDisplayName
                 })
        {
            definitions.GetSettingDefinition(name).Scopes.HasFlag(SettingScopes.Tenant).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task Should_Send_With_The_Tenant_Smtp_Host_When_The_Tenant_Has_One()
    {
        var settingManager = Resolve<ISettingManager>();
        await settingManager.ChangeSettingForApplicationAsync(EmailSettingNames.Smtp.Host, "host-smtp.example.com");

        LoginAsDefaultTenantAdmin();
        await settingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.Value, EmailSettingNames.Smtp.Host, "tenant-smtp.example.com");

        Resolve<ISmtpEmailSenderConfiguration>().Host.ShouldBe("tenant-smtp.example.com");
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Host_Smtp_Host_When_The_Tenant_Has_None()
    {
        var settingManager = Resolve<ISettingManager>();
        await settingManager.ChangeSettingForApplicationAsync(EmailSettingNames.Smtp.Host, "host-smtp.example.com");

        LoginAsDefaultTenantAdmin();

        Resolve<ISmtpEmailSenderConfiguration>().Host.ShouldBe("host-smtp.example.com");
    }

    [Fact]
    public async Task Should_Keep_Host_Mail_On_The_Host_Settings()
    {
        var settingManager = Resolve<ISettingManager>();
        await settingManager.ChangeSettingForApplicationAsync(EmailSettingNames.Smtp.Host, "host-smtp.example.com");

        LoginAsDefaultTenantAdmin();
        await settingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.Value, EmailSettingNames.Smtp.Host, "tenant-smtp.example.com");

        LoginAsHostAdmin();

        Resolve<ISmtpEmailSenderConfiguration>().Host.ShouldBe("host-smtp.example.com");
    }
}
