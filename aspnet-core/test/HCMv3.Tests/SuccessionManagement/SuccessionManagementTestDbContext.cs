using HCMv3.SuccessionManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.SuccessionManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the SuccessionManagement app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new SuccessionManagement entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class SuccessionManagementTestDbContext : DbContext
{
    public DbSet<ImpactOfLoss> ImpactOfLosses { get; set; }
    public DbSet<NineBoxRating> NineBoxRatings { get; set; }
    public DbSet<Ranking> Rankings { get; set; }
    public DbSet<Readiness> Readinesses { get; set; }
    public DbSet<ReasonForLeaving> ReasonForLeavings { get; set; }
    public DbSet<RiskOfLoss> RiskOfLosses { get; set; }
    public DbSet<SuccessionRating> SuccessionRatings { get; set; }
    public DbSet<TalentPool> TalentPools { get; set; }

    public DbSet<NineBoxMapping> NineBoxMappings { get; set; }
    public DbSet<EmpPerformanceHistory> EmpPerformanceHistories { get; set; }
    public DbSet<EmpPotentialAssessment> EmpPotentialAssessments { get; set; }
    public DbSet<EmpTalentMatrixData> EmpTalentMatrixDatas { get; set; }
    public DbSet<EmployeeTalentFlag> EmployeeTalentFlags { get; set; }
    public DbSet<SuccessorNominee> SuccessorNominees { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public SuccessionManagementTestDbContext(DbContextOptions<SuccessionManagementTestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SuccessorNominee has two distinct FKs to Employee (Employee/Nominee) -- disambiguate explicitly
        // so Sqlite's model build doesn't try to infer a single shared relationship.
        modelBuilder.Entity<SuccessorNominee>(b =>
        {
            b.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.Nominee).WithMany().HasForeignKey(e => e.NomineeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
