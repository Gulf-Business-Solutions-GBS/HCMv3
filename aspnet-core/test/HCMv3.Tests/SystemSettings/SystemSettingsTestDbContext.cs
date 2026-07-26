using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.SystemSettings;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the SystemSettings app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new SystemSettings entities have no table in the shared test Sqlite
/// database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.TrainingManagement.TrainingManagementTestDbContext</c>.
/// </summary>
public class SystemSettingsTestDbContext : DbContext
{
    public DbSet<ManageAppraisalSettings> ManageAppraisalSettings { get; set; }
    public DbSet<ManageDocumentSettings> ManageDocumentSettings { get; set; }
    public DbSet<ManageFIPostingSettings> ManageFIPostingSettings { get; set; }
    public DbSet<ManageGeneralSettings> ManageGeneralSettings { get; set; }
    public DbSet<ManageNotificationSettings> ManageNotificationSettings { get; set; }
    public DbSet<ManageOrgSettings> ManageOrgSettings { get; set; }
    public DbSet<ManagePayrollSettings> ManagePayrollSettings { get; set; }

    public DbSet<ManagePersonnelSettings> ManagePersonnelSettings { get; set; }
    public DbSet<ManageProjectCostingSettings> ManageProjectCostingSettings { get; set; }
    public DbSet<ManageReportSettings> ManageReportSettings { get; set; }
    public DbSet<ManageSecuritySettings> ManageSecuritySettings { get; set; }
    public DbSet<ManageSelfServiceSettings> ManageSelfServiceSettings { get; set; }
    public DbSet<ManageTimeSettings> ManageTimeSettings { get; set; }
    public DbSet<ManageWorkflowSettings> ManageWorkflowSettings { get; set; }
    public DbSet<PayrollDriver> PayrollDrivers { get; set; }

    public DbSet<HCMv3.GeneralMasterInfo.Country> Countries { get; set; }

    public SystemSettingsTestDbContext(DbContextOptions<SystemSettingsTestDbContext> options) : base(options)
    {
    }
}
