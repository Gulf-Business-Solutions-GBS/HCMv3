using HCMv3.CompetencyDevelopment;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.CompetencyDevelopment;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the CompetencyDevelopment app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new CompetencyDevelopment entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider) each test spins up its own private
/// in-memory Sqlite connection against this context and wires fake
/// <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see <see cref="FakeRepository"/>)
/// on top of it via NSubstitute. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class CompetencyDevelopmentTestDbContext : DbContext
{
    public DbSet<CompetencyType> CompetencyTypes { get; set; }
    public DbSet<Competency> Competencies { get; set; }
    public DbSet<CompetencyByJobTitle> CompetencyByJobTitles { get; set; }
    public DbSet<CompetencyByJobTitleDetail> CompetencyByJobTitleDetails { get; set; }
    public DbSet<CDPeriod> CDPeriods { get; set; }
    public DbSet<CDForm> CDForms { get; set; }
    public DbSet<CDPlan> CDPlans { get; set; }
    public DbSet<CDMidYearReview> CDMidYearReviews { get; set; }
    public DbSet<CDAnnualYearReview> CDAnnualYearReviews { get; set; }
    public DbSet<CDRating> CDRatings { get; set; }

    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public CompetencyDevelopmentTestDbContext(DbContextOptions<CompetencyDevelopmentTestDbContext> options) : base(options)
    {
    }
}
