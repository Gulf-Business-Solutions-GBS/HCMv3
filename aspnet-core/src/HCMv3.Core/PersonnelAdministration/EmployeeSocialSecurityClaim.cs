using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee social-security claim/reimbursement record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSocialSecurityClaim</c>, Wave 3 deferred
/// bucket).
/// </summary>
public class EmployeeSocialSecurityClaim : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime ClaimDate { get; set; }

    public virtual double EmployeeContributionAmount { get; set; }

    public virtual double EmployerContributionAmount { get; set; }
}
