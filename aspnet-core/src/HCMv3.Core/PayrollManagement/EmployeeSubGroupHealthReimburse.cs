using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range dependant-tier health-reimbursement entitlement attached directly to an
/// <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupHealthReimburse</c>), listed separately
/// and linked back via <see cref="EmployeeSubGroupId"/>. Structurally near-identical to
/// <see cref="EmployeeSubGroupBenefitDetail"/> but kept as its own distinct entity since it is not nested
/// under a Benefit header.
/// </summary>
public class EmployeeSubGroupHealthReimburse : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int DependantTypeId { get; set; }

    public virtual DependantType DependantType { get; set; }

    public virtual double Amount { get; set; }

    public virtual double AgeLimit { get; set; }
}
