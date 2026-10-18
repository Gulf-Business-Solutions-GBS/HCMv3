using HCMv3.TrainingManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.TrainingManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the TrainingManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new TrainingManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class TrainingManagementTestDbContext : DbContext
{
    public DbSet<TrainingCourse> TrainingCourses { get; set; }
    public DbSet<TrainingSchedule> TrainingSchedules { get; set; }
    public DbSet<EmployeeTraining> EmployeeTrainings { get; set; }
    public DbSet<TrainingAttendance> TrainingAttendances { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.EmployeeCertification> EmployeeCertifications { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Institution> Institutions { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Country> Countries { get; set; }

    public TrainingManagementTestDbContext(DbContextOptions<TrainingManagementTestDbContext> options) : base(options)
    {
    }
}
