using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// The tenant landing page is now Home, at /app/main/home, and its screen key follows.
    ///
    /// A DATA MIGRATION IS REQUIRED, not just a catalogue edit. Screen rows are keyed by ScreenKey and
    /// the seeder matches on it, so a renamed key looks like a brand new screen: it would INSERT a second
    /// row, leave the old one behind, and -- because STN003 was still taken by that old row -- hand Home
    /// a different code. The code people have started to learn would change for no reason anybody could
    /// see.
    ///
    /// Favourites, recents and shared quick links all point at the key as well, so renaming it in one
    /// place and not the others would silently detach every shortcut to the landing page.
    ///
    /// Updates rather than deletes and re-seeds: the row carries the tenant's own code mapping and the
    /// shortcuts hang off it.
    /// </summary>
    /// <remarks>
    /// Attributes carried here rather than in a Designer file. This migration touches DATA ONLY -- no
    /// column, table or index changes -- so it needs no model snapshot, and scaffolding one would have
    /// produced eleven thousand lines of unchanged model to say nothing. EF discovers a migration by
    /// these two attributes; without them it is compiled and silently never run.
    /// </remarks>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260904120000_Rename_Dashboard_Screen_To_Home")]
    public partial class Rename_Dashboard_Screen_To_Home : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Screens
                   SET ScreenKey = 'app.main.home',
                       Route     = '/app/main/home',
                       NameKey   = 'Home'
                 WHERE ScreenKey = 'app.main.dashboard';

                UPDATE UserScreenUsages
                   SET ScreenKey = 'app.main.home'
                 WHERE ScreenKey = 'app.main.dashboard';

                UPDATE SharedQuickLinks
                   SET ScreenKey = 'app.main.home'
                 WHERE ScreenKey = 'app.main.dashboard';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Screens
                   SET ScreenKey = 'app.main.dashboard',
                       Route     = '/app/main/dashboard',
                       NameKey   = 'Dashboard'
                 WHERE ScreenKey = 'app.main.home';

                UPDATE UserScreenUsages
                   SET ScreenKey = 'app.main.dashboard'
                 WHERE ScreenKey = 'app.main.home';

                UPDATE SharedQuickLinks
                   SET ScreenKey = 'app.main.dashboard'
                 WHERE ScreenKey = 'app.main.home';
            ");
        }
    }
}
