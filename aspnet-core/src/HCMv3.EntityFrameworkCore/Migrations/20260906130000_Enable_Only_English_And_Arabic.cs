using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Leaves English (UK) and Arabic enabled and disables the other thirteen.
    ///
    /// WHY A MIGRATION AND NOT THE SEEDER. The seeder creates languages on a BRAND NEW database, so
    /// changing it fixed nothing on any environment that already exists -- the server still offered all
    /// fifteen, because its rows were created before the change. Every deployment needs the same two
    /// languages, so this belongs where deployments pick it up: a migration that runs once, everywhere,
    /// with nobody clicking through thirteen screens.
    ///
    /// DISABLED, NOT DELETED. The rows stay, so an administrator can re-enable one from
    /// Administration > Languages without a developer, and the language texts already stored against it
    /// survive. Deleting would take those with it.
    ///
    /// Deliberately not idempotent-by-intent: it runs once. If somebody enables French next month, this
    /// migration will not fight them, because migrations do not run twice.
    ///
    /// The thirteen carry no translations here anyway. Choosing one gave an English interface under a
    /// foreign flag, which reads as a broken product rather than an unfinished translation.
    /// </summary>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260906130000_Enable_Only_English_And_Arabic")]
    public partial class Enable_Only_English_And_Arabic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE AbpLanguages
                   SET IsDisabled = 1
                 WHERE Name NOT IN ('en-GB', 'ar')
                   AND IsDisabled = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Puts every language back on. Down is for undoing the deployment, not for expressing a
            // preference, so it restores the state the migration found rather than some subset of it.
            migrationBuilder.Sql(@"
                UPDATE AbpLanguages
                   SET IsDisabled = 0
                 WHERE IsDisabled = 1;
            ");
        }
    }
}
