using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound delegation of an Employee's duties to another Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDelegation</c>).
/// </summary>
public class EmployeeDelegation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DelegateToId { get; set; }

    public virtual Employee DelegateTo { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }
}
