using HCMv3.GeneralMasterInfo;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// Standalone SQLite context for the appraisal review tests: the 360 appraisal, its step settings, the goal
/// form and its timelines, and the workflow rows the review reads.
///
/// <para>
/// Its own context rather than the shared <c>PerformanceManagementTestDbContext</c>, for the same reason that
/// one exists: the new tables are not in <c>HCMv3DbContext</c> yet (the DbSets are added by hand, outside this
/// work), so the integration database has nothing to put them in. Kept separate so the existing harness is
/// untouched.
/// </para>
/// </summary>
public class AppraisalReviewTestDbContext : DbContext
{
    public DbSet<AppraisalStepSetting> AppraisalStepSettings { get; set; }
    public DbSet<AppraisalStepSettingRelationship> AppraisalStepSettingRelationships { get; set; }

    public DbSet<PAEmployee> PAEmployees { get; set; }
    public DbSet<PAEmployeeDetail> PAEmployeeDetails { get; set; }
    public DbSet<PAEmployeeReviewer> PAEmployeeReviewers { get; set; }
    public DbSet<PAEmployeeRating> PAEmployeeRatings { get; set; }
    public DbSet<PACycle> PACycles { get; set; }
    public DbSet<FormType> FormTypes { get; set; }
    public DbSet<PARating> PARatings { get; set; }
    public DbSet<KpiInfo> KpiInfos { get; set; }
    public DbSet<KpiCategory> KpiCategories { get; set; }
    public DbSet<KpiByJobTitle> KpiByJobTitles { get; set; }
    public DbSet<KpiByJobTitleDetail> KpiByJobTitleDetails { get; set; }
    public DbSet<KpiByOrgUnit> KpiByOrgUnits { get; set; }
    public DbSet<KpiByOrgUnitDetail> KpiByOrgUnitDetails { get; set; }

    public DbSet<PAForm> PAForms { get; set; }
    public DbSet<PAPeriod> PAPeriods { get; set; }
    public DbSet<PATimeline> PATimelines { get; set; }
    public DbSet<PATimelineException> PATimelineExceptions { get; set; }
    public DbSet<PAPeerNominee> PAPeerNominees { get; set; }

    public DbSet<WorkflowRequest> WorkflowRequests { get; set; }
    public DbSet<WorkFlowApprover> WorkFlowApprovers { get; set; }
    public DbSet<RequestMaster> RequestMasters { get; set; }
    public DbSet<RequestDetail> RequestDetails { get; set; }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeManager> EmployeeManagers { get; set; }
    public DbSet<EmployeeRelationship> EmployeeRelationships { get; set; }
    public DbSet<RelationshipType> RelationshipTypes { get; set; }
    public DbSet<Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }

    public AppraisalReviewTestDbContext(DbContextOptions<AppraisalReviewTestDbContext> options) : base(options)
    {
    }

    /// <summary>The same model trims as <c>PerformanceManagementTestDbContext</c>, for the same reasons.</summary>
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
