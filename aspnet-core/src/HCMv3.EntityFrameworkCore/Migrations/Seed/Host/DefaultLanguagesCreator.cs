using System.Collections.Generic;
using System.Linq;
using Abp.Localization;
using Microsoft.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Host;

public class DefaultLanguagesCreator
{
    public static List<ApplicationLanguage> InitialLanguages => GetInitialLanguages();

    private readonly HCMv3DbContext _context;

    private static List<ApplicationLanguage> GetInitialLanguages()
    {
        var tenantId = HCMv3Consts.MultiTenancyEnabled ? null : (int?)1;

        // ALL FIFTEEN ARE CREATED, and all but English and Arabic are then DISABLED by the
        // Enable_Only_English_And_Arabic and Use_Neutral_English_Instead_Of_En_GB migrations. Only those
        // two are offered, because the other thirteen have no translations here and choosing one gave an
        // English interface under a foreign flag.
        //
        // English is the neutral "en", not "en-GB". The culture code has to stay neutral for the base
        // localization dictionary to resolve -- "en-GB" served only the 937-key stub file and left the
        // interface showing raw key names. British date formatting is applied on the client instead, via
        // the "en" -> "en-GB" locale mappings in the Angular appconfig files, so the flag here is GB
        // while the code stays neutral.
        //
        // Created and hidden, rather than never created: a language that does not exist cannot be turned
        // back on. An administrator who wants French enables the row from Administration > Languages,
        // with no developer and no deployment. Seeding only two would have made a fresh database
        // permanently different from an upgraded one, and taken that choice away.
        return new List<ApplicationLanguage>
            {
                new ApplicationLanguage(tenantId, "en", "English", "famfamfam-flags gb"),
                new ApplicationLanguage(tenantId, "en-GB", "English (UK)", "famfamfam-flags gb"),
                new ApplicationLanguage(tenantId, "ar", "العربية", "famfamfam-flags sa"),
                new ApplicationLanguage(tenantId, "de", "Deutsch", "famfamfam-flags de"),
                new ApplicationLanguage(tenantId, "it", "Italiano", "famfamfam-flags it"),
                new ApplicationLanguage(tenantId, "fr", "Français", "famfamfam-flags fr"),
                new ApplicationLanguage(tenantId, "pt-BR", "Português (Brasil)", "famfamfam-flags br"),
                new ApplicationLanguage(tenantId, "tr", "Türkçe", "famfamfam-flags tr"),
                new ApplicationLanguage(tenantId, "ru", "Pусский", "famfamfam-flags ru"),
                new ApplicationLanguage(tenantId, "zh-Hans", "简体中文", "famfamfam-flags cn"),
                new ApplicationLanguage(tenantId, "es-MX", "Español (México)", "famfamfam-flags mx"),
                new ApplicationLanguage(tenantId, "es", "Español (Spanish)", "famfamfam-flags es"),
                new ApplicationLanguage(tenantId, "vi", "Tiếng Việt", "famfamfam-flags vn"),
                new ApplicationLanguage(tenantId, "nl", "Dutch (Nederlands)", "famfamfam-flags nl"),
                new ApplicationLanguage(tenantId, "th", "ภาษาไทย", "famfamfam-flags th"),
            };
    }

    public DefaultLanguagesCreator(HCMv3DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateLanguages();
    }

    private void CreateLanguages()
    {
        foreach (var language in InitialLanguages)
        {
            AddLanguageIfNotExists(language);
        }
    }

    private void AddLanguageIfNotExists(ApplicationLanguage language)
    {
        if (_context.Languages.IgnoreQueryFilters().Any(l => l.TenantId == language.TenantId && l.Name == language.Name))
        {
            return;
        }

        _context.Languages.Add(language);

        _context.SaveChanges();
    }
}

