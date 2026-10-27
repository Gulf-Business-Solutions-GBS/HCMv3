using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee dependant record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDependant</c>, Wave 3 deferred bucket).
///
/// <see cref="DependantTypeId"/> FKs <c>HCMv3.GeneralMasterInfo.DependantType</c> (already ported).
/// <see cref="TicketTypeId"/> FKs <c>HCMv3.PayrollManagement.TicketType</c> (already ported) -- legacy
/// reuses the air-ticket TicketType lookup to represent "which ticket benefit this dependant is entitled
/// to" (Code "1" reserved for the employee themselves, excluded from the legacy dependant picklist; that
/// UI-level filtering concern is not ported).
///
/// The legacy overlap/duplicate validation against other <c>EmployeeDependant</c> rows for the same
/// employee+TicketType, and the auto-end-dating cascade, are ported at the app-service level, not on the
/// entity (same approach as other Wave 1/2/3 effective-dated ledger entities).
/// </summary>
public class EmployeeDependant : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameAr { get; set; }

    public virtual int DependantTypeId { get; set; }

    public virtual DependantType DependantType { get; set; }

    public virtual DateTime BirthDate { get; set; }

    public virtual int TicketTypeId { get; set; }

    public virtual TicketType TicketType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual bool SpecialNeed { get; set; }

    public virtual bool IncludeInSocialSecurity { get; set; }

    public virtual bool IncludeInHealthInsurance { get; set; }
}
