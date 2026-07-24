using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel feedback/claim record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelFeedback</c>).
///
/// DELIBERATE SIMPLIFICATION (Applicant drop): same rationale as <see cref="Travel"/> -- the legacy
/// either-or <c>Employee</c>/<c>Applicant</c> relationship is dropped; <see cref="EmployeeId"/> is the only
/// party field (required). <see cref="TravelId"/> is filtered by Employee only (the legacy
/// <c>EmpTravel</c> XPCollection branched on whichever of Employee/Applicant was set; since Applicant is
/// dropped, only the Employee branch applies).
///
/// DELIBERATE SIMPLIFICATION (DocAttachment drop): the legacy <c>AfterConstruction()</c> eagerly created a
/// <c>DocAttachment</c> child record. <c>DocAttachment</c> is confirmed absent everywhere else in this
/// codebase (dropped in every other module that referenced it), so it is dropped here too, along with the
/// eager-creation side effect.
/// </summary>
public class TravelFeedback : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? TravelId { get; set; }

    public virtual Travel Travel { get; set; }

    public virtual DateTime ClaimDate { get; set; }

    public virtual double ClaimAmount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
