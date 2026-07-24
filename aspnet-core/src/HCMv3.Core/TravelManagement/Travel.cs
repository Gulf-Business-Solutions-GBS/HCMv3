using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel header (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.Travel</c>).
///
/// DELIBERATE SIMPLIFICATION (Applicant drop): the legacy entity had an either-or
/// <c>Employee</c>/<c>Applicant</c> relationship (a Travel record could belong to either an existing
/// Employee or a job Applicant from the never-ported Recruitment module). This port drops the
/// <c>Applicant</c> FK entirely and makes <see cref="EmployeeId"/> the only party field (required, not
/// nullable) -- meaning this port only supports travel records for existing employees, not job applicants.
/// This is an acceptable scope reduction given Recruitment is out of scope for this codebase.
///
/// DELIBERATE SIMPLIFICATION (TotalTravelCost rollup): legacy <c>UpdateTotalAmount()</c> eagerly recomputed
/// this field on every save by summing child <see cref="TravelDetail.TotalAmount"/> rows. Following the
/// established precedent in this codebase for identical header/detail amount rollups (see
/// <c>PersonnelAdministration.EmployeeBasicPay.Amount</c> and its detail app service), this field is kept as
/// a stored, but NOT client-editable (absent from <c>CreateOrEditTravelDto</c>), column -- the legacy
/// cross-entity cascade recompute is deliberately NOT re-implemented here.
/// </summary>
public class Travel : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TravelTypeId { get; set; }

    public virtual TravelType TravelType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>Legacy display name "Total Travel Cost". See class remarks -- not client-editable.</summary>
    public virtual double TotalTravelCost { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
