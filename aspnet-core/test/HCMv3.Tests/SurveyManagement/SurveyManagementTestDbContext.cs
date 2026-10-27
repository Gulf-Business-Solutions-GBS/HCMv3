using HCMv3.SurveyManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.SurveyManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the SurveyManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new SurveyManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as
/// <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class SurveyManagementTestDbContext : DbContext
{
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyDetail> SurveyDetails { get; set; }
    public DbSet<EmployeeSurvey> EmployeeSurveys { get; set; }
    public DbSet<EmployeeSurveyDetail> EmployeeSurveyDetails { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public SurveyManagementTestDbContext(DbContextOptions<SurveyManagementTestDbContext> options) : base(options)
    {
    }
}
