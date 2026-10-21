using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Swaps the enabled English language from "en-GB" back to "en", and points the default language
    /// setting at it.
    ///
    /// WHY. Enable_Only_English_And_Arabic left "en-GB" enabled, and that silently broke the English
    /// interface. The localization dictionaries are keyed by culture: HCMv3.xml declares culture="en"
    /// and carries all 5,860 keys, while HCMv3-en-GB.xml is a stub with 937. Selecting "en-GB" served
    /// only the stub -- Home, Employee, LoanType and GoLiveSetup all came back missing, so screens
    /// showed raw key names. The framework's own sources collapsed the same way, returning zero keys
    /// for Abp, AbpWeb and AbpZero. Under "en" every source resolves, because "en" IS the base
    /// dictionary's culture rather than a child of it.
    ///
    /// DATE FORMAT. "en" is a neutral culture, so .NET would otherwise format dates the American way.
    /// Dates are rendered on the client, not the server, and the Angular locale mappings in
    /// appconfig.json now map "en" to "en-GB" for luxon, Angular and PrimeNG. The interface therefore
    /// reads English from the full dictionary and writes dates as dd/MM/yyyy. The two concerns are
    /// separated on purpose: the language picks the words, the locale mapping picks the format.
    ///
    /// "en-GB" is disabled rather than deleted, on the same reasoning as the migration before it: a
    /// language that does not exist cannot be turned back on.
    /// </summary>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260907090000_Use_Neutral_English_Instead_Of_En_GB")]
    public partial class Use_Neutral_English_Instead_Of_En_GB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- English is British in everything but the culture code: the code has to stay neutral
                -- for the dictionary to resolve, so the flag carries the country instead.
                UPDATE AbpLanguages
                   SET IsDisabled = 0,
                       DisplayName = 'English',
                       Icon = 'famfamfam-flags gb'
                 WHERE Name = 'en';

                UPDATE AbpLanguages
                   SET IsDisabled = 1
                 WHERE Name NOT IN ('en', 'ar')
                   AND IsDisabled = 0;

                -- Anyone still pointed at a language that is now switched off would get a broken
                -- interface, so drop the preference and let them fall back to the default.
                DELETE FROM AbpSettings
                 WHERE Name = 'Abp.Localization.DefaultLanguageName'
                   AND (TenantId IS NOT NULL OR UserId IS NOT NULL)
                   AND Value NOT IN ('en', 'ar');

                -- The host-level default is the one every unauthenticated visitor gets.
                UPDATE AbpSettings
                   SET Value = 'en'
                 WHERE Name = 'Abp.Localization.DefaultLanguageName'
                   AND TenantId IS NULL
                   AND UserId IS NULL;

                INSERT INTO AbpSettings (TenantId, UserId, Name, Value, CreationTime)
                SELECT NULL, NULL, 'Abp.Localization.DefaultLanguageName', 'en', GETDATE()
                 WHERE NOT EXISTS (
                       SELECT 1 FROM AbpSettings
                        WHERE Name = 'Abp.Localization.DefaultLanguageName'
                          AND TenantId IS NULL
                          AND UserId IS NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores what this migration found: en-GB enabled and default, en switched off again.
            migrationBuilder.Sql(@"
                UPDATE AbpLanguages SET IsDisabled = 0 WHERE Name = 'en-GB';
                UPDATE AbpLanguages SET IsDisabled = 1 WHERE Name = 'en';

                UPDATE AbpSettings
                   SET Value = 'en-GB'
                 WHERE Name = 'Abp.Localization.DefaultLanguageName'
                   AND TenantId IS NULL
                   AND UserId IS NULL;
            ");
        }
    }
}
