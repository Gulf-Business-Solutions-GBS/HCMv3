using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Aggregated child of <see cref="Project"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectWageType</c>), linked back via
/// <see cref="ProjectId"/>. Legacy <c>PerformValidation</c> parity: a given <see cref="WageTypeId"/> must not
/// already be assigned to the same <see cref="ProjectId"/> -- enforced in
/// <c>ProjectWageTypesAppService</c>, not here.
/// </summary>
public class ProjectWageType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
