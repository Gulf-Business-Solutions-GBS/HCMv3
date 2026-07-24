using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Manual leave quota adjustment record (encashment / adjustment / fractional / no-quota-impact), ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsAdjustment</c>,
/// Wave 1 deferred bucket.
///
/// The legacy cascade side effects (updating <see cref="EmployeeAbsenceQuota.QuotaAdjusted"/> on
/// save/delete, negative-balance validation against the linked quota record) are ported at the app-service
/// level in <c>EmployeeAbsAdjustmentsAppService</c>, not on the entity.
/// </summary>
public class EmployeeAbsAdjustment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual AdjustmentType AdjustmentType { get; set; }

    public virtual DateTime EffectiveDate { get; set; }

    public virtual double QuotaAdjusted { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual AdjustmentPaymentType AdjustmentPaymentType { get; set; }
}
