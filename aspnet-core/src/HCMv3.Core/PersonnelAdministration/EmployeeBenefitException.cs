using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee benefit exception record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeBenefitException</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="ExceptionBenefitType"/> reuses the <see cref="PersonnelAdministration.ExceptionBenefitType"/>
/// enum already ported in Wave 3 (on <c>EmployeeDependantException</c>) -- not redeclared here.
/// </summary>
public class EmployeeBenefitException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual ExceptionBenefitType ExceptionBenefitType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }
}
