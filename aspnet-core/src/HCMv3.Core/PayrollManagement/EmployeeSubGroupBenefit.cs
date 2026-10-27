using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Benefit-type entitlement header for an <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupBenefit</c>), listed separately and
/// linked back via <see cref="EmployeeSubGroupId"/>. Its own child is <see cref="EmployeeSubGroupBenefitDetail"/>.
/// </summary>
public class EmployeeSubGroupBenefit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int BenefitTypeId { get; set; }

    public virtual BenefitType BenefitType { get; set; }

    public virtual double MaxAmount { get; set; }
}
