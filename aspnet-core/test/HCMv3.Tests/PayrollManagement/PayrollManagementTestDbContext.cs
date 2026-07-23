using HCMv3.PayrollManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the PayrollManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new PayrollManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider — a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute — not via the real Abp repository/DI pipeline,
/// which is untouched. Same pattern as <c>HCMv3.Tests.TimeManagement.TimeManagementTestDbContext</c>.
/// </summary>
public class PayrollManagementTestDbContext : DbContext
{
    public DbSet<AXBatch> AXBatches { get; set; }
    public DbSet<AbsenceType> AbsenceTypes { get; set; }
    public DbSet<AbsenceTypeException> AbsenceTypeExceptions { get; set; }
    public DbSet<AbsenceDeductionRule> AbsenceDeductionRules { get; set; }
    public DbSet<AbsenceDedRuleDetail> AbsenceDedRuleDetails { get; set; }
    public DbSet<AbsenceViolationRule> AbsenceViolationRules { get; set; }
    public DbSet<AbsenceViolationRuleDetail> AbsenceViolationRuleDetails { get; set; }
    public DbSet<AbsenceViolationRuleException> AbsenceViolationRuleExceptions { get; set; }
    public DbSet<AttendanceType> AttendanceTypes { get; set; }
    public DbSet<AccountType> AccountTypes { get; set; }
    public DbSet<AccountInfo> AccountInfos { get; set; }
    public DbSet<EmployeeGroup> EmployeeGroups { get; set; }
    public DbSet<PayrollReasonType> PayrollReasonTypes { get; set; }

    public DbSet<EmployeeSubGroup> EmployeeSubGroups { get; set; }
    public DbSet<EmployeeSubGroupAbsence> EmployeeSubGroupAbsences { get; set; }
    public DbSet<EmployeeSubGroupAbsenceType> EmployeeSubGroupAbsenceTypes { get; set; }
    public DbSet<EmployeeSubGroupAssetAllowance> EmployeeSubGroupAssetAllowances { get; set; }
    public DbSet<EmployeeSubGroupBenefit> EmployeeSubGroupBenefits { get; set; }
    public DbSet<EmployeeSubGroupBenefitDetail> EmployeeSubGroupBenefitDetails { get; set; }
    public DbSet<EmployeeSubGroupBonus> EmployeeSubGroupBonuses { get; set; }
    public DbSet<EmployeeSubGroupCommission> EmployeeSubGroupCommissions { get; set; }
    public DbSet<EmployeeSubGroupFund> EmployeeSubGroupFunds { get; set; }
    public DbSet<EmployeeSubGroupGratuity> EmployeeSubGroupGratuities { get; set; }
    public DbSet<EmployeeSubGroupHealthReimburse> EmployeeSubGroupHealthReimburses { get; set; }
    public DbSet<EmployeeSubGroupPayment> EmployeeSubGroupPayments { get; set; }
    public DbSet<EmployeeSubGroupTicket> EmployeeSubGroupTickets { get; set; }
    public DbSet<EmployeeSubGroupWageType> EmployeeSubGroupWageTypes { get; set; }

    public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
    public DbSet<PayrollArea> PayrollAreas { get; set; }
    public DbSet<PayrollCalendar> PayrollCalendars { get; set; }
    public DbSet<PayrollControlRecord> PayrollControlRecords { get; set; }
    public DbSet<IndemnityPayRule> IndemnityPayRules { get; set; }
    public DbSet<IndemnityPayRuleDetail> IndemnityPayRuleDetails { get; set; }
    public DbSet<OrgUnitWageAccGroup> OrgUnitWageAccGroups { get; set; }
    public DbSet<WageAccGroup> WageAccGroups { get; set; }
    public DbSet<WageAccGroupDetail> WageAccGroupDetails { get; set; }
    public DbSet<WageType> WageTypes { get; set; }
    public DbSet<WageTypeException> WageTypeExceptions { get; set; }
    public DbSet<WageTypeLeave> WageTypeLeaves { get; set; }
    public DbSet<WageTypeSecurityRole> WageTypeSecurityRoles { get; set; }
    public DbSet<WageAccount> WageAccounts { get; set; }

    public DbSet<SocialSecurity> SocialSecurities { get; set; }
    public DbSet<SocialSecurityDetail> SocialSecurityDetails { get; set; }
    public DbSet<SocialSecurityMaritalAllowance> SocialSecurityMaritalAllowances { get; set; }
    public DbSet<SocialSecurityEducationAllowance> SocialSecurityEducationAllowances { get; set; }
    public DbSet<SocialSecurityRule> SocialSecurityRules { get; set; }
    public DbSet<TicketClass> TicketClasses { get; set; }
    public DbSet<TicketPaxType> TicketPaxTypes { get; set; }
    public DbSet<TicketRoute> TicketRoutes { get; set; }
    public DbSet<TicketFare> TicketFares { get; set; }
    public DbSet<TicketType> TicketTypes { get; set; }

    public DbSet<EmployeeWageAccGroup> EmployeeWageAccGroups { get; set; }
    public DbSet<EmployeeWageAccount> EmployeeWageAccounts { get; set; }
    public DbSet<EmployeePayrollStatus> EmployeePayrollStatuses { get; set; }
    public DbSet<AbsencePayment> AbsencePayments { get; set; }
    public DbSet<TempBenefitType> TempBenefitTypes { get; set; }
    public DbSet<TempEmployeeBenefit> TempEmployeeBenefits { get; set; }
    public DbSet<WageTypeRegister> WageTypeRegisters { get; set; }
    public DbSet<PayrollLog> PayrollLogs { get; set; }

    public DbSet<CostRegister> CostRegisters { get; set; }
    public DbSet<CostRegisterDetail> CostRegisterDetails { get; set; }
    public DbSet<SalaryRegister> SalaryRegisters { get; set; }
    public DbSet<SalaryRegisterDetail> SalaryRegisterDetails { get; set; }
    public DbSet<SalaryRegisterDaysDetail> SalaryRegisterDaysDetails { get; set; }
    public DbSet<SimulationSalaryRegister> SimulationSalaryRegisters { get; set; }
    public DbSet<SimulationSalaryRegisterDetail> SimulationSalaryRegisterDetails { get; set; }
    public DbSet<SimulationSalaryRegisterDaysDetail> SimulationSalaryRegisterDaysDetails { get; set; }
    public DbSet<AbsenceViolationActionRecord> AbsenceViolationActionRecords { get; set; }
    public DbSet<AbsenceViolationActionLog> AbsenceViolationActionLogs { get; set; }
    public DbSet<GratuityAccrualSummary> GratuityAccrualSummaries { get; set; }
    public DbSet<LeaveAccrualSummary> LeaveAccrualSummaries { get; set; }
    public DbSet<TicketAccrualSummary> TicketAccrualSummaries { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.BenefitType> BenefitTypes { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.DependantType> DependantTypes { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Currency> Currencies { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Country> Countries { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.EducationBranch> EducationBranches { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.MaritalStatus> MaritalStatuses { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Education> Educations { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Qualification> Qualifications { get; set; }
    public DbSet<HCMv3.OrganizationManagement.CostElement> CostElements { get; set; }
    public DbSet<HCMv3.Authorization.Roles.Role> Roles { get; set; }
    public DbSet<Abp.Organizations.OrganizationUnit> OrganizationUnits { get; set; }

    public PayrollManagementTestDbContext(DbContextOptions<PayrollManagementTestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// This bespoke minimal context (unlike the real <c>HCMv3DbContext</c>, which derives from ABP Zero's
    /// <c>AbpZeroDbContext</c> and already has the necessary fluent configuration baked in) doesn't know how
    /// to resolve the self-referencing Creator/LastModifier/Deleter-user audit navigation properties that
    /// ABP Zero's <c>AbpUser</c>/<c>AbpRole</c> base classes declare on <c>User</c>/<c>Role</c> (ambiguous:
    /// multiple FKs on the same entity point back at the same principal type). Since this wave's
    /// <see cref="WageTypeSecurityRole"/> only needs the <c>RoleId</c> FK + a simple Role lookup (not the
    /// full user-audit chain), those specific navigation properties are ignored here so the model can build.
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
