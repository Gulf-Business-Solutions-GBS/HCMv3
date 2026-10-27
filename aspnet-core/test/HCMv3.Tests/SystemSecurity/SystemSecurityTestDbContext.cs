using HCMv3.SystemSecurity;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.SystemSecurity;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the SystemSecurity app-service unit tests. Same
/// pattern as HCMv3.Tests.TrainingManagement.TrainingManagementTestDbContext -- the shared HCMv3DbContext
/// is intentionally not touched by this wave (DbSet wiring/migrations left for a follow-up).
/// </summary>
public class SystemSecurityTestDbContext : DbContext
{
    public DbSet<SysUserOrgUnitData> SysUserOrgUnitData { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailToken> EmailTokens { get; set; }
    public DbSet<PayslipConfig> PayslipConfigs { get; set; }
    public DbSet<NotificationMaster> NotificationMasters { get; set; }
    public DbSet<SystemNotification> SystemNotifications { get; set; }
    public DbSet<SysObject> SysObjects { get; set; }
    public DbSet<RetroReasonLog> RetroReasonLogs { get; set; }
    public DbSet<PunchNotificationLog> PunchNotificationLogs { get; set; }
    public DbSet<BulkUploadLog> BulkUploadLogs { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }
    public DbSet<EmailNotificationLog> EmailNotificationLogs { get; set; }
    public DbSet<SysTenantConfig> SysTenantConfigs { get; set; }
    public DbSet<TaskEvent> TaskEvents { get; set; }
    public DbSet<EmployeesNotification> EmployeesNotifications { get; set; }
    public DbSet<EmployeesNotificationLog> EmployeesNotificationLogs { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.NotificationConfig> NotificationConfigs { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<HCMv3.OrganizationManagement.OrganizationUnitType> OrganizationUnitTypes { get; set; }
    public DbSet<HCMv3.Authorization.Roles.Role> Roles { get; set; }

    public SystemSecurityTestDbContext(DbContextOptions<SystemSecurityTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// See HCMv3.Tests.GovtRelationsManagement.GovtRelationsManagementTestDbContext remarks for why the
    /// self-referencing User/Role audit navigation properties need to be ignored for this bespoke minimal
    /// context to build its model (SysUserOrgUnitData FKs Role, which pulls in User).
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

        // PunchNotificationLog.EmployeeDutyPeriodId is a loose scalar reference for this bespoke test
        // model (EmployeeDutyPeriod, from TimeManagement, is out of this wave's scope to fully wire up
        // here -- its own FKs pull in Duty/HolidayCalendar). Ignoring the navigation keeps
        // EmployeeDutyPeriodId as a plain int column with no FK constraint, same as the real entity's
        // loose EntityObjectId-style references elsewhere in this module.
        modelBuilder.Entity<PunchNotificationLog>(b =>
        {
            b.Ignore(e => e.EmployeeDutyPeriod);
        });
    }
}
