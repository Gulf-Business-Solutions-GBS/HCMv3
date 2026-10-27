using System.Linq;
using Abp.Configuration;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Net.Mail;
using Microsoft.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Host;

public class DefaultSettingsCreator
{
    private readonly HCMv3DbContext _context;

    public DefaultSettingsCreator(HCMv3DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        int? tenantId = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (!HCMv3Consts.MultiTenancyEnabled)
#pragma warning disable 162
        {
            tenantId = MultiTenancyConsts.DefaultTenantId;
        }
#pragma warning restore 162

        //Emailing
        AddSettingIfNotExists(EmailSettingNames.DefaultFromAddress, "admin@mydomain.com", tenantId);
        AddSettingIfNotExists(EmailSettingNames.DefaultFromDisplayName, "mydomain.com mailer", tenantId);

        //Languages
        AddSettingIfNotExists(LocalizationSettingNames.DefaultLanguage, "en", tenantId);
    }

    private void AddSettingIfNotExists(string name, string value, int? tenantId = null)
    {
        if (_context.Settings.IgnoreQueryFilters().Any(s => s.Name == name && s.TenantId == tenantId && s.UserId == null))
        {
            return;
        }

        _context.Settings.Add(new Setting(tenantId, null, name, value));
        _context.SaveChanges();
    }
}

