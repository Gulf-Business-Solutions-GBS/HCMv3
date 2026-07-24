using HCMv3.AccommodationManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.AccommodationManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the AccommodationManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new AccommodationManagement entities have no table in the shared test
/// Sqlite database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider — a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute — not via the real Abp repository/DI pipeline,
/// which is untouched. Same pattern as <c>HCMv3.Tests.PayrollManagement.PayrollManagementTestDbContext</c>.
/// </summary>
public class AccommodationManagementTestDbContext : DbContext
{
    public DbSet<Location> Locations { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<BuildingContract> BuildingContracts { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Flat> Flats { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Bed> Beds { get; set; }
    public DbSet<AccommodationType> AccommodationTypes { get; set; }
    public DbSet<AccommodationServiceType> AccommodationServiceTypes { get; set; }
    public DbSet<EmployeeAccommodation> EmployeeAccommodations { get; set; }
    public DbSet<AccommodationServices> AccommodationServices { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public AccommodationManagementTestDbContext(DbContextOptions<AccommodationManagementTestDbContext> options) : base(options)
    {
    }
}
