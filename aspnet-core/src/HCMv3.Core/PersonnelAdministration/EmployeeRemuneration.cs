using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee remuneration record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeRemuneration</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="WageTypeId"/> FKs <c>HCMv3.PayrollManagement.WageType</c> (already ported). Legacy field is
/// optional (no <c>[Required]</c>/null-check in <c>PerformValidations</c>), so ported as nullable.
/// </summary>
public class EmployeeRemuneration : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual double PaymentNumber { get; set; }

    public virtual PaymentUnit PaymentUnit { get; set; }

    public virtual string Comments { get; set; }
}
