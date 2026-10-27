using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Manual leave quota forfeiture record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsQuotaForfeit</c>, Wave 1 deferred
/// bucket).
///
/// The legacy cascade side effects (updating <see cref="EmployeeAbsenceQuota.QuotaForfeited"/> on
/// save/delete, negative-balance validation against the linked quota record) are ported at the app-service
/// level in <c>EmployeeAbsQuotaForfeitsAppService</c>, not on the entity. The legacy non-persistent
/// <c>DontUpdateQuotaBalance</c> checkbox is a request-only field on the create/edit DTO, not a column.
/// </summary>
public class EmployeeAbsQuotaForfeit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual DateTime EffectiveDate { get; set; }

    public virtual double QuotaForfeited { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
