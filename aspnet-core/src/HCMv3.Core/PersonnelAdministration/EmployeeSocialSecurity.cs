using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee social-security registration record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSocialSecurity</c>, Wave 3 deferred
/// bucket). <see cref="SocialSecurityNum"/> is <c>long</c> per legacy.
///
/// The legacy auto-end-dating cascade (each new record automatically closes the previous open-ended
/// record's <c>EndDate</c> to the day before the new record's <c>StartDate</c>) is ported at the
/// app-service level, not on the entity (same approach as other Wave 1/2/3 effective-dated ledger entities).
/// </summary>
public class EmployeeSocialSecurity : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual long SocialSecurityNum { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual bool SocialSecurityContrib { get; set; }
}
