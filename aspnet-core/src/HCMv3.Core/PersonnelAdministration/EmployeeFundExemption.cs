using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee fund exemption period record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeFundExemption</c>, Wave 2 deferred
/// bucket). The legacy no-overlap validation for the same EmployeeFund is present but commented out
/// (dead code) in the legacy source and is therefore NOT ported.
/// </summary>
public class EmployeeFundExemption : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeFundId { get; set; }

    public virtual EmployeeFund EmployeeFund { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
