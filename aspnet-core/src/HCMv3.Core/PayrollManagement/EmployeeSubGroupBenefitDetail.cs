using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range dependant-tier detail of an <see cref="EmployeeSubGroupBenefit"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupBenefitDetail</c>), listed separately
/// and linked back via <see cref="EmployeeSubGroupBenefitId"/> -- nested one level deeper than its 12
/// siblings, which attach directly to <see cref="EmployeeSubGroup"/>.
/// </summary>
public class EmployeeSubGroupBenefitDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupBenefitId { get; set; }

    public virtual EmployeeSubGroupBenefit EmployeeSubGroupBenefit { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int DependantTypeId { get; set; }

    public virtual DependantType DependantType { get; set; }

    public virtual double Amount { get; set; }

    public virtual double AgeLimit { get; set; }
}
