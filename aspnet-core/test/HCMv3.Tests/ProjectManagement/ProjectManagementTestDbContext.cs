using HCMv3.ProjectManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the ProjectManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new ProjectManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as
/// <c>HCMv3.Tests.PayrollManagement.PayrollManagementTestDbContext</c>.
/// </summary>
public class ProjectManagementTestDbContext : DbContext
{
    public DbSet<ProjectType> ProjectTypes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectWageType> ProjectWageTypes { get; set; }
    public DbSet<EmployeeProject> EmployeeProjects { get; set; }
    public DbSet<EmployeeProjectDetail> EmployeeProjectDetails { get; set; }
    public DbSet<ProjectRegister> ProjectRegisters { get; set; }
    public DbSet<ProjectRegisterDetail> ProjectRegisterDetails { get; set; }
    public DbSet<ProjectLog> ProjectLogs { get; set; }

    public DbSet<HCMv3.PayrollManagement.WageType> WageTypes { get; set; }
    public DbSet<HCMv3.TimeManagement.TimeRegister> TimeRegisters { get; set; }

    // TimeRegister navigation properties -- included so the model builds (same pattern as
    // HCMv3.Tests.TimeManagement.TimeManagementTestDbContext).
    public DbSet<HCMv3.TimeManagement.DutyPeriod> DutyPeriods { get; set; }
    public DbSet<HCMv3.TimeManagement.Duty> Duties { get; set; }
    public DbSet<HCMv3.TimeManagement.TimePolicy> TimePolicies { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.OvertimeType> OvertimeTypes { get; set; }
    public DbSet<HCMv3.TimeManagement.BreakShift> BreakShifts { get; set; }
    public DbSet<HCMv3.TimeManagement.HolidayCalendar> HolidayCalendars { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public ProjectManagementTestDbContext(DbContextOptions<ProjectManagementTestDbContext> options) : base(options)
    {
    }
}
