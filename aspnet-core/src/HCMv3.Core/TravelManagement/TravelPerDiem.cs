using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.TravelManagement;

/// <summary>
/// Per-diem rate table (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelPerDiem</c>). Rate for a From/To
/// <see cref="TravelDestination"/> pair, scoped to an <see cref="PayrollManagement.EmployeeGroup"/> /
/// <see cref="PayrollManagement.EmployeeSubGroup"/> combination. Legacy <c>PerformValidation</c> parity
/// (uniqueness across FromDestination/ToDestination/EmployeeGroup/EmployeeSubGroup) is enforced in
/// <c>TravelPerDiemsAppService</c>, not here. The legacy <c>EmpSubGroup</c> XPCollection (UI dropdown filter
/// re-scoping EmployeeSubGroup options to the selected EmployeeGroup) is presentation logic and not ported.
/// </summary>
public class TravelPerDiem : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int FromDestinationId { get; set; }

    public virtual TravelDestination FromDestination { get; set; }

    public virtual int ToDestinationId { get; set; }

    public virtual TravelDestination ToDestination { get; set; }

    public virtual int EmployeeGroupId { get; set; }

    public virtual EmployeeGroup EmployeeGroup { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual double Amount { get; set; }
}
