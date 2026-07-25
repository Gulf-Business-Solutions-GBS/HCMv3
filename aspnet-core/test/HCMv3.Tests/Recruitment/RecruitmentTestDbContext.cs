using HCMv3.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.Recruitment;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the Recruitment app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new Recruitment entities have no table in the shared test Sqlite database
/// used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/ToListAsync/AnyAsync
/// require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c> does not support
/// them), each test spins up its own private in-memory Sqlite connection against this context and wires fake
/// <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see <see cref="FakeRepository"/>) on
/// top of it via NSubstitute -- not via the real Abp repository/DI pipeline, which is untouched. Same pattern
/// as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class RecruitmentTestDbContext : DbContext
{
    public DbSet<Applicant> Applicants { get; set; }
    public DbSet<ApplicantCV> ApplicantCVs { get; set; }
    public DbSet<CVQualification> CVQualifications { get; set; }
    public DbSet<CVWorkExp> CVWorkExps { get; set; }
    public DbSet<CVSkill> CVSkills { get; set; }
    public DbSet<CVLanguage> CVLanguages { get; set; }
    public DbSet<Vacancy> Vacancies { get; set; }
    public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
    public DbSet<InterviewDetails> InterviewDetails { get; set; }
    public DbSet<OnbGroup> OnbGroups { get; set; }
    public DbSet<OnbGroupDetail> OnbGroupDetails { get; set; }
    public DbSet<OnbTask> OnbTasks { get; set; }
    public DbSet<OnbTemplate> OnbTemplates { get; set; }
    public DbSet<OnbTemplateDetails> OnbTemplateDetails { get; set; }
    public DbSet<OnbApplicantTasks> OnbApplicantTasks { get; set; }
    public DbSet<OnbAppTaskComments> OnbAppTaskComments { get; set; }
    public DbSet<OnbVideoLink> OnbVideoLinks { get; set; }

    // Referenced lookup/master entities from other modules -- included so the model builds.
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Title> Titles { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.MaritalStatus> MaritalStatuses { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Religion> Religions { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Country> Countries { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Qualification> Qualifications { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.LanguageInfo> LanguageInfos { get; set; }
    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }
    public DbSet<HCMv3.OrganizationManagement.WorkLocation> WorkLocations { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }

    public RecruitmentTestDbContext(DbContextOptions<RecruitmentTestDbContext> options) : base(options)
    {
    }
}
