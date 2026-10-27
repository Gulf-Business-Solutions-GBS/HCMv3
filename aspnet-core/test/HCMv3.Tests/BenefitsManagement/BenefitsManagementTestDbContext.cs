using HCMv3.BenefitsManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.BenefitsManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the BenefitsManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new BenefitsManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync/SumAsync require an async-capable LINQ provider -- a plain
/// <c>List&lt;T&gt;.AsQueryable()</c> does not support them), each test spins up its own private
/// in-memory Sqlite connection against this context and wires fake
/// <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see <see cref="FakeRepository"/>)
/// on top of it via NSubstitute -- not via the real Abp repository/DI pipeline, which is untouched. Same
/// pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
///
/// <see cref="EmployeeInsurance.Dependant"/> pulls in the <c>EmployeeDependant</c> entity, which in turn
/// FKs <c>DependantType</c> and <c>TicketType</c> (which FKs <c>WageType</c>) -- all included here purely
/// so the EF model builds (the app service <c>Include()</c>s the Dependant navigation); none of these
/// tests populate/exercise that optional navigation directly.
/// </summary>
public class BenefitsManagementTestDbContext : DbContext
{
    public DbSet<InsurancePlan> InsurancePlans { get; set; }
    public DbSet<InsuranceProvider> InsuranceProviders { get; set; }
    public DbSet<EmployeeInsurance> EmployeeInsurances { get; set; }
    public DbSet<EmployeeInsuranceDetail> EmployeeInsuranceDetails { get; set; }
    public DbSet<CompanyInsuranceDetail> CompanyInsuranceDetails { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.EmployeeDependant> EmployeeDependants { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.DependantType> DependantTypes { get; set; }
    public DbSet<HCMv3.PayrollManagement.TicketType> TicketTypes { get; set; }
    public DbSet<HCMv3.PayrollManagement.WageType> WageTypes { get; set; }

    public BenefitsManagementTestDbContext(DbContextOptions<BenefitsManagementTestDbContext> options) : base(options)
    {
    }
}
