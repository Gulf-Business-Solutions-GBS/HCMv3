using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Aggregated child of <see cref="ProjectRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectRegisterDetail</c>), linked back via
/// <see cref="ProjectRegisterId"/>. Legacy <c>PerformValidation</c> was an empty no-op, so no business
/// validation is ported here or in <c>ProjectRegisterDetailsAppService</c> beyond plain CRUD.
/// </summary>
public class ProjectRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ProjectRegisterId { get; set; }

    public virtual ProjectRegister ProjectRegister { get; set; }

    public virtual int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Number { get; set; }

    public virtual double Amount { get; set; }
}
