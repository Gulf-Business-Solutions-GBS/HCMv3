using HCMv3.PerformanceManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the PerformanceManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new PerformanceManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.GovtRelationsManagement.GovtRelationsManagementTestDbContext</c>.
/// </summary>
public class PerformanceManagementTestDbContext : DbContext
{
    public DbSet<FormType> FormTypes { get; set; }
    public DbSet<KpiByJobTitle> KpiByJobTitles { get; set; }
    public DbSet<KpiByJobTitleDetail> KpiByJobTitleDetails { get; set; }
    public DbSet<KpiByOrgUnit> KpiByOrgUnits { get; set; }
    public DbSet<KpiByOrgUnitDetail> KpiByOrgUnitDetails { get; set; }
    public DbSet<KpiCategory> KpiCategories { get; set; }
    public DbSet<KpiInfo> KpiInfos { get; set; }
    public DbSet<PAAnnualYearReview> PAAnnualYearReviews { get; set; }
    public DbSet<PACycle> PACycles { get; set; }
    public DbSet<PAEmployee> PAEmployees { get; set; }
    public DbSet<PAEmployeeDetail> PAEmployeeDetails { get; set; }
    public DbSet<PAEmployeeRating> PAEmployeeRatings { get; set; }
    public DbSet<PAEmployeeReviewer> PAEmployeeReviewers { get; set; }
    public DbSet<PAForm> PAForms { get; set; }
    public DbSet<PAGoal> PAGoals { get; set; }
    public DbSet<PAMidYearReview> PAMidYearReviews { get; set; }
    public DbSet<PAPeerNominee> PAPeerNominees { get; set; }
    public DbSet<PAPeerReview> PAPeerReviews { get; set; }
    public DbSet<PAPeriod> PAPeriods { get; set; }
    public DbSet<PAPlan> PAPlans { get; set; }
    public DbSet<PAQuarter1Review> PAQuarter1Reviews { get; set; }
    public DbSet<PAQuarter3Review> PAQuarter3Reviews { get; set; }
    public DbSet<PARating> PARatings { get; set; }
    public DbSet<PATimeline> PATimelines { get; set; }
    public DbSet<PATimelineException> PATimelineExceptions { get; set; }

    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public PerformanceManagementTestDbContext(DbContextOptions<PerformanceManagementTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// See <c>GovtRelationsManagementTestDbContext</c> remarks for why the self-referencing User/Role audit
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
