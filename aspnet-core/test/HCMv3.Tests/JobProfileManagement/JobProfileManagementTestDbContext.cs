using HCMv3.JobProfileManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.JobProfileManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the JobProfileManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new JobProfileManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class JobProfileManagementTestDbContext : DbContext
{
    public DbSet<JobFamily> JobFamilies { get; set; }
    public DbSet<JobFunction> JobFunctions { get; set; }
    public DbSet<JobResponsibility> JobResponsibilities { get; set; }
    public DbSet<EducationCertificate> EducationCertificates { get; set; }
    public DbSet<EducationDegree> EducationDegrees { get; set; }
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
    public DbSet<Skill> Skills { get; set; }

    public DbSet<JobDescription> JobDescriptions { get; set; }
    public DbSet<JobDescriptionCertificate> JobDescriptionCertificates { get; set; }
    public DbSet<JobDescriptionDegree> JobDescriptionDegrees { get; set; }
    public DbSet<JobDescriptionInterviewQuestion> JobDescriptionInterviewQuestions { get; set; }
    public DbSet<JobDescriptionResponsibility> JobDescriptionResponsibilities { get; set; }
    public DbSet<JobDescriptionSkill> JobDescriptionSkills { get; set; }
    public DbSet<JobDescriptionCompetency> JobDescriptionCompetencies { get; set; }
    public DbSet<JobDescriptionAcknowledgement> JobDescriptionAcknowledgements { get; set; }

    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<HCMv3.CompetencyDevelopment.Competency> Competencies { get; set; }
    public DbSet<HCMv3.CompetencyDevelopment.CompetencyType> CompetencyTypes { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public JobProfileManagementTestDbContext(DbContextOptions<JobProfileManagementTestDbContext> options) : base(options)
    {
    }
}
