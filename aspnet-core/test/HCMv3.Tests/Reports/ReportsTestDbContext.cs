using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.Reports;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the payslip (Reports) app-service unit tests.
/// Same rationale/pattern as <c>HCMv3.Tests.PayrollManagement.PayrollManagementTestDbContext</c>: the real
/// <c>HCMv3DbContext</c> must not be touched by this wave, and the app service under test needs a real
/// async-capable LINQ provider (Include/ToListAsync/FirstOrDefaultAsync).
/// </summary>
public class ReportsTestDbContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeOrgAssignment> EmployeeOrgAssignments { get; set; }
    public DbSet<EmployeeBank> EmployeeBanks { get; set; }
    public DbSet<EmployeeBasicPay> EmployeeBasicPays { get; set; }
    public DbSet<EmployeeBasicPayDetail> EmployeeBasicPayDetails { get; set; }

    public DbSet<SalaryRegister> SalaryRegisters { get; set; }
    public DbSet<SalaryRegisterDetail> SalaryRegisterDetails { get; set; }
    public DbSet<WageType> WageTypes { get; set; }
    public DbSet<PayrollReasonType> PayrollReasonTypes { get; set; }
    public DbSet<PayrollArea> PayrollAreas { get; set; }

    public DbSet<ManagePayrollSettings> ManagePayrollSettings { get; set; }
    public DbSet<PayslipConfig> PayslipConfigs { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Currency> Currencies { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Bank> Banks { get; set; }
    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<HCMv3.Authorization.Roles.Role> Roles { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }

    public ReportsTestDbContext(DbContextOptions<ReportsTestDbContext> options) : base(options)
    {
    }

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
