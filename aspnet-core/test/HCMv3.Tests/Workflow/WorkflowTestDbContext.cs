using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the Workflow app-service unit tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new Workflow entities have no table in the shared test Sqlite database
/// used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/ToListAsync/
/// AnyAsync require an async-capable LINQ provider), each test spins up its own private in-memory Sqlite
/// connection against this context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/>
/// instances (see <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp
/// repository/DI pipeline, which is untouched. Same pattern as
/// <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class WorkflowTestDbContext : DbContext
{
    public DbSet<WorkflowRole> WorkflowRoles { get; set; }
    public DbSet<WorkflowGroup> WorkflowGroups { get; set; }
    public DbSet<WorkflowGroupDetail> WorkflowGroupDetails { get; set; }
    public DbSet<WorkflowGroupFieldAccess> WorkflowGroupFieldAccesses { get; set; }
    public DbSet<RequestMaster> RequestMasters { get; set; }
    public DbSet<RequestDetail> RequestDetails { get; set; }
    public DbSet<RequestDetailParallel> RequestDetailParallels { get; set; }
    public DbSet<WorkflowRequest> WorkflowRequests { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<WorkFlowApprover> WorkFlowApprovers { get; set; }
    public DbSet<WorkflowRemark> WorkflowRemarks { get; set; }
    public DbSet<WFReadLog> WFReadLogs { get; set; }
    public DbSet<EmployeeWorkflowRole> EmployeeWorkflowRoles { get; set; }

    public DbSet<HCMv3.OrganizationManagement.OrganizationUnitType> OrganizationUnitTypes { get; set; }
    public DbSet<HCMv3.OrganizationManagement.JobTitle> JobTitles { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.PersonnelAdministration.EmployeeManager> EmployeeManagers { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public WorkflowTestDbContext(DbContextOptions<WorkflowTestDbContext> options) : base(options)
    {
    }
}
