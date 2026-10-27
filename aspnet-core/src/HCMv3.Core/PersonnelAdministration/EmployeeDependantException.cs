using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Benefit exception (override) record for a specific <see cref="EmployeeDependant"/> (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDependantException</c>, Wave 3
/// deferred bucket).
///
/// The legacy overlap validation against other exception rows for the same dependant+ExceptionBenefitType,
/// and the date-range-within-parent-dependant validation, are ported at the app-service level, not on the
/// entity (same approach as other Wave 1/2/3 ledger entities).
/// </summary>
public class EmployeeDependantException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeDependantId { get; set; }

    public virtual EmployeeDependant EmployeeDependant { get; set; }

    public virtual ExceptionBenefitType ExceptionBenefitType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double Amount { get; set; }
}
