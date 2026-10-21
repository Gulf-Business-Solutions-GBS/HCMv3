using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Minimal in-memory Sqlite context for the cost-allocation / journal-voucher tests.
///
/// <para>A DEDICATED context (rather than adding DbSets to the shared
/// <see cref="PayrollManagementTestDbContext"/>) because the costing wave needs several entities that
/// context does not carry -- <see cref="EmployeeBasicPay"/>, <see cref="EmployeeBasicPayDetail"/>,
/// <see cref="EmployeeOrgAssignment"/>, <see cref="OrgUnitProfile"/>, <see cref="ProjectLog"/>,
/// <see cref="ExecutionLog"/> -- and that shared file is being edited concurrently by sibling waves.
/// Same "one bespoke context per wave" precedent as <c>TimeManagementTestDbContext</c> and
/// <c>WorkflowTestDbContext</c>. The real <c>HCMv3DbContext</c> is deliberately untouched.</para>
/// </summary>
public class CostingTestDbContext : DbContext
{
    public DbSet<PayrollArea> PayrollAreas { get; set; }
    public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
    public DbSet<PayrollReasonType> PayrollReasonTypes { get; set; }
    public DbSet<Nationality> Nationalities { get; set; }
    public DbSet<AXBatch> AXBatches { get; set; }
    public DbSet<AccountType> AccountTypes { get; set; }
    public DbSet<AccountInfo> AccountInfos { get; set; }
    public DbSet<WageType> WageTypes { get; set; }
    public DbSet<CostRegister> CostRegisters { get; set; }
    public DbSet<CostRegisterDetail> CostRegisterDetails { get; set; }
    public DbSet<SalaryRegister> SalaryRegisters { get; set; }
    public DbSet<SalaryRegisterDetail> SalaryRegisterDetails { get; set; }
    public DbSet<EmployeeWageAccount> EmployeeWageAccounts { get; set; }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeBasicPay> EmployeeBasicPays { get; set; }
    public DbSet<EmployeeBasicPayDetail> EmployeeBasicPayDetails { get; set; }
    public DbSet<EmployeeOrgAssignment> EmployeeOrgAssignments { get; set; }

    public DbSet<OrgUnitProfile> OrgUnitProfiles { get; set; }
    public DbSet<CostElement> CostElements { get; set; }
    public DbSet<Currency> Currencies { get; set; }

    public DbSet<ProjectLog> ProjectLogs { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }

    /// <summary>For the finance screens' "Current" period (legacy SetCurrentDates).</summary>
    public DbSet<PayrollControlRecord> PayrollControlRecords { get; set; }

    public CostingTestDbContext(DbContextOptions<CostingTestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Same reason as PayrollManagementTestDbContext: this bespoke context can't resolve ABP Zero's
        // self-referencing Creator/LastModifier/Deleter user audit navigations, and the costing tests
        // don't need them.
        modelBuilder.Ignore<Abp.Authorization.Users.UserPermissionSetting>();
        modelBuilder.Ignore<Abp.Authorization.Roles.RolePermissionSetting>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserLogin>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserLoginAttempt>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserRole>();
        modelBuilder.Ignore<Abp.Authorization.Users.UserOrganizationUnit>();
        modelBuilder.Ignore<HCMv3.Authorization.Users.User>();
        modelBuilder.Ignore<HCMv3.Authorization.Roles.Role>();
    }
}
