using HCMv3.GatePassManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.GatePassManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the GatePassManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new GatePassManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class GatePassManagementTestDbContext : DbContext
{
    public DbSet<GatePassMovement> GatePassMovements { get; set; }
    public DbSet<GatePassStatus> GatePassStatuses { get; set; }
    public DbSet<GatePassType> GatePassTypes { get; set; }
    public DbSet<PersonType> PersonTypes { get; set; }
    public DbSet<VehicleBrand> VehicleBrands { get; set; }
    public DbSet<VehicleModel> VehicleModels { get; set; }
    public DbSet<VehicleType> VehicleTypes { get; set; }
    public DbSet<GatePassVendor> GatePassVendors { get; set; }
    public DbSet<GatePass> GatePasses { get; set; }
    public DbSet<GatePassMaterial> GatePassMaterials { get; set; }
    public DbSet<GatePassPerson> GatePassPersons { get; set; }
    public DbSet<GatePassVehicle> GatePassVehicles { get; set; }
    public DbSet<PoolCar> PoolCars { get; set; }
    public DbSet<EmployeePoolCar> EmployeePoolCars { get; set; }

    public DbSet<HCMv3.OrganizationManagement.WorkLocation> WorkLocations { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public GatePassManagementTestDbContext(DbContextOptions<GatePassManagementTestDbContext> options) : base(options)
    {
    }
}
