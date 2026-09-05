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

        // ENGLISH (UK) AND ARABIC ONLY. The ASP.NET Zero template seeds fifteen languages, and the other
        // thirteen have no translations here -- picking one gave you an English interface with a foreign
        // flag over it. A new deployment should offer only the two this product is actually written in.
        //
        // This runs on a FRESH database. An existing installation already has all fifteen rows; disable
        // the rest in Administration > Languages, which is where an administrator would add one back.
        return new List<ApplicationLanguage>
            {
                new ApplicationLanguage(tenantId, "en-GB", "English (UK)", "famfamfam-flags gb"),
                new ApplicationLanguage(tenantId, "ar", "العربية", "famfamfam-flags sa"),
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

