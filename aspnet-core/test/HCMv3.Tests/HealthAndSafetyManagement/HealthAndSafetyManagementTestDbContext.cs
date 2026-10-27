using HCMv3.HealthAndSafetyManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.HealthAndSafetyManagement;

/// <summary>
/// Minimal, standalone EF Core DbContext used ONLY by the HealthAndSafetyManagement app-service unit
/// tests.
///
/// This wave's task explicitly forbids touching the real <c>HCMv3DbContext</c> (DbSet wiring/migrations are
/// left for a follow-up), so the new HealthAndSafetyManagement entities have no table in the shared test
/// Sqlite database used by <c>AppTestBase</c>. To still exercise real EF Core query behavior (CountAsync/
/// ToListAsync/AnyAsync require an async-capable LINQ provider -- a plain <c>List&lt;T&gt;.AsQueryable()</c>
/// does not support them), each test spins up its own private in-memory Sqlite connection against this
/// context and wires fake <see cref="Abp.Domain.Repositories.IRepository{TEntity}"/> instances (see
/// <see cref="FakeRepository"/>) on top of it via NSubstitute -- not via the real Abp repository/DI
/// pipeline, which is untouched. Same pattern as <c>HCMv3.Tests.ProjectManagement.ProjectManagementTestDbContext</c>.
/// </summary>
public class HealthAndSafetyManagementTestDbContext : DbContext
{
    public DbSet<ActionStatus> ActionStatuses { get; set; }
    public DbSet<FirstAidMedicine> FirstAidMedicines { get; set; }
    public DbSet<IncidentClaimStatus> IncidentClaimStatuses { get; set; }
    public DbSet<IncidentNature> IncidentNatures { get; set; }
    public DbSet<IncidentSeverity> IncidentSeverities { get; set; }
    public DbSet<IncidentStatus> IncidentStatuses { get; set; }
    public DbSet<IncidentType> IncidentTypes { get; set; }
    public DbSet<InvestigationRootCause> InvestigationRootCauses { get; set; }
    public DbSet<RiskAction> RiskActions { get; set; }
    public DbSet<RiskImpact> RiskImpacts { get; set; }
    public DbSet<RiskPossibility> RiskPossibilities { get; set; }
    public DbSet<RiskStatus> RiskStatuses { get; set; }
    public DbSet<RiskType> RiskTypes { get; set; }
    public DbSet<SafetyAreaAsset> SafetyAreaAssets { get; set; }

    public DbSet<ActionRegister> ActionRegisters { get; set; }
    public DbSet<FirstAidBox> FirstAidBoxes { get; set; }
    public DbSet<FirstAidBoxDetail> FirstAidBoxDetails { get; set; }
    public DbSet<IncidentClaim> IncidentClaims { get; set; }
    public DbSet<IncidentInvestigation> IncidentInvestigations { get; set; }
    public DbSet<IncidentRegister> IncidentRegisters { get; set; }
    public DbSet<RiskRegister> RiskRegisters { get; set; }
    public DbSet<SafetyChecklist> SafetyChecklists { get; set; }
    public DbSet<SiteAudit> SiteAudits { get; set; }
    public DbSet<SiteAuditDetail> SiteAuditDetails { get; set; }

    public DbSet<HCMv3.OrganizationManagement.WorkLocation> WorkLocations { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Currency> Currencies { get; set; }

    public DbSet<HCMv3.PersonnelAdministration.Employee> Employees { get; set; }
    public DbSet<HCMv3.GeneralMasterInfo.Nationality> Nationalities { get; set; }

    public HealthAndSafetyManagementTestDbContext(DbContextOptions<HealthAndSafetyManagementTestDbContext> options) : base(options)
    {
    }
}
