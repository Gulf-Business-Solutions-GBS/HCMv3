using HCMv3.ManpowerPlanningManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.ManpowerPlanningManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the ManpowerPlanningManagement app-service unit
/// tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new ManpowerPlanningManagement entities have no table in the shared test
/// Sqlite database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior
/// (CountAsync/ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain
/// <c>List&lt;T&gt;.AsQueryable()</c> does not support them), each test spins up its own private in-memory
/// Sqlite connection against this context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/>
/// instances (see <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp
/// repository/DI pipeline, which is untouched. Same pattern as
/// <c>HCMv3.Tests.GovtRelationsManagement.GovtRelationsManagementTestDbContext</c>.
/// </summary>
public class ManpowerPlanningManagementTestDbContext : DbContext
{
    public DbSet<ManpowerPlanningPeriod> ManpowerPlanningPeriods { get; set; }
    public DbSet<HCMv3.ManpowerPlanningManagement.ManpowerPlanning> ManpowerPlannings { get; set; }
    public DbSet<ManpowerPlanningDetail> ManpowerPlanningDetails { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PositionRelationship> PositionRelationships { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<HCMv3.PayrollManagement.EmployeeGroup> EmployeeGroups { get; set; }
    public DbSet<HCMv3.PayrollManagement.EmployeeSubGroup> EmployeeSubGroups { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }

    public ManpowerPlanningManagementTestDbContext(DbContextOptions<ManpowerPlanningManagementTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// See <c>PayrollManagementTestDbContext</c> remarks for why the self-referencing User/Role audit
    /// navigation properties need to be ignored for this bespoke minimal context to build its model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ManpowerPlanningStatus is derived from WorkflowStatus and has no column -- same as the real
        // HCMv3DbContext.
        modelBuilder.Entity<HCMv3.ManpowerPlanningManagement.ManpowerPlanning>(b => b.Ignore(e => e.Status));

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
