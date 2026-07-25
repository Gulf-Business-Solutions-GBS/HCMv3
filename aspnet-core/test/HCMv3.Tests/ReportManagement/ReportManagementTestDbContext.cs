using HCMv3.ReportManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the ReportManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new ReportManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider) each test spins up its own private
/// in-memory Sqlite connection against this context and wires fake
/// <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see <see cref="FakeRepository"/>)
/// on top of it via NSubstitute. Same pattern as <c>HCMv3.Tests.PayrollManagement.PayrollManagementTestDbContext</c>.
/// </summary>
public class ReportManagementTestDbContext : DbContext
{
    public DbSet<DomainType> DomainTypes { get; set; }
    public DbSet<ReportType> ReportTypes { get; set; }
    public DbSet<PrintReport> PrintReports { get; set; }
    public DbSet<PrintReportPermission> PrintReportPermissions { get; set; }
    public DbSet<PrintReportPermissionDetail> PrintReportPermissionDetails { get; set; }
    public DbSet<ReportMaster> ReportMasters { get; set; }
    public DbSet<ReportDetail> ReportDetails { get; set; }
    public DbSet<ReportSetting> ReportSettings { get; set; }
    public DbSet<ReportSettingDetail> ReportSettingDetails { get; set; }
    public DbSet<ReportInfo> ReportInfos { get; set; }
    public DbSet<ReportInfoDetail> ReportInfoDetails { get; set; }

    public DbSet<HCMv3.Authorization.Roles.Role> Roles { get; set; }

    public ReportManagementTestDbContext(DbContextOptions<ReportManagementTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// This bespoke minimal context doesn't know how to resolve the self-referencing Creator/LastModifier/
    /// Deleter-user audit navigation properties that ABP Zero's <c>AbpRole</c> base class declares on
    /// <c>Role</c> (ambiguous: multiple FKs on the same entity point back at the same principal type). Since
    /// <see cref="PrintReportPermission"/> only needs the <c>RoleId</c> FK + a simple Role lookup, those
    /// specific navigation properties are ignored here so the model can build. Same pattern as
    /// <c>HCMv3.Tests.PayrollManagement.PayrollManagementTestDbContext</c>.
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
