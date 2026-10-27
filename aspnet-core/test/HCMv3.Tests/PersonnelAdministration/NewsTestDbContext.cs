using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the <see cref="News"/> app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so <see cref="News"/> has no table in the shared test Sqlite database used by
/// <c>AppTestBase</c> (unlike the rest of PersonnelAdministration, already wired there). To still exercise
/// real EF Core query behavior (CountAsync/ToListAsync/AnyAsync require an async-capable LINQ provider),
/// this context spins up its own private in-memory Sqlite connection and wires fake
/// <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see <see cref="NewsFakeRepository"/>)
/// on top of it via NSubstitute. Same pattern as <c>HCMv3.Tests.SystemSecurity.SystemSecurityTestDbContext</c>.
/// </summary>
public class NewsTestDbContext : DbContext
{
    public DbSet<News> News { get; set; }

    public DbSet<HCMv3.GeneralMasterInfo.NewsType> NewsTypes { get; set; }
    public DbSet<HCMv3.OrganizationManagement.OrganizationUnitType> OrganizationUnitTypes { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }

    public NewsTestDbContext(DbContextOptions<NewsTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// See <c>SystemSecurityTestDbContext</c> remarks for why the self-referencing User/Role audit
    /// navigation properties need to be ignored for this bespoke minimal context to build its model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<Abp.Authorization.Users.UserPermissionSetting>();
        modelBuilder.Ignore<Abp.Authorization.Roles.RolePermissionSetting>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserLogin>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserLoginAttempt>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserRole>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserOrganizationUnit>();

        modelBuilder.Entity<HCMv3.Authorization.Users.User>(b =>
        {
            b.Ignore("CreatorUser");
            b.Ignore("LastModifierUser");
            b.Ignore("DeleterUser");
            b.Ignore("Permissions");
            b.Ignore("Roles");
            b.Ignore("Logins");
            b.Ignore("Claims");
            b.Ignore("Tokens");
            b.Ignore("Settings");
            b.Ignore("OrganizationUnits");
        });

        modelBuilder.Entity<HCMv3.Authorization.Roles.Role>(b =>
        {
            b.Ignore("CreatorUser");
            b.Ignore("LastModifierUser");
            b.Ignore("DeleterUser");
            b.Ignore("Permissions");
        });
    }
}
