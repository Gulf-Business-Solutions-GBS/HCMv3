using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Payroll-period/run tracking log row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.WageTypeRegister</c>). DESPITE ITS NAME, this is NOT a
/// child/detail table of <see cref="WageType"/> -- the legacy class has no WageType field at all; it's
/// really a payroll-run log entry (conceptually alongside <see cref="PayrollLog"/>). <see cref="EmployeeId"/>
/// is nullable: the legacy field carries no <c>[Required]</c>/not-null constraint, and run-level log
/// messages may have no associated employee. No business validation is ported (the legacy has none beyond
/// plain CRUD).
/// </summary>
public class WageTypeRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int ForPeriod { get; set; }

    public virtual int InPeriod { get; set; }
}
