using HCMv3.TravelManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the TravelManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new TravelManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class TravelManagementTestDbContext : DbContext
{
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<TravelAirline> TravelAirlines { get; set; }
    public DbSet<TravelCancelReason> TravelCancelReasons { get; set; }
    public DbSet<TravelDestination> TravelDestinations { get; set; }
    public DbSet<TravelType> TravelTypes { get; set; }
    public DbSet<TravelPerDiem> TravelPerDiems { get; set; }
    public DbSet<Travel> Travels { get; set; }
    public DbSet<TravelDetail> TravelDetails { get; set; }
    public DbSet<TravelDetailCost> TravelDetailCosts { get; set; }
    public DbSet<TravelFeedback> TravelFeedbacks { get; set; }

    public DbSet<HCMv3.PayrollManagement.WageType> WageTypes { get; set; }
    public DbSet<HCMv3.PayrollManagement.EmployeeGroup> EmployeeGroups { get; set; }
    public DbSet<HCMv3.PayrollManagement.EmployeeSubGroup> EmployeeSubGroups { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public TravelManagementTestDbContext(DbContextOptions<TravelManagementTestDbContext> options) : base(options)
    {
    }
}
