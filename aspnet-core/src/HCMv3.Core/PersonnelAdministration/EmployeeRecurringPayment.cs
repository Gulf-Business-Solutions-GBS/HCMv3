using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee recurring payment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeRecurringPayment</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="WageTypeId"/> FKs <c>HCMv3.PayrollManagement.WageType</c> (already ported).
/// <see cref="PayInterval"/> reuses <c>HCMv3.PayrollManagement.PayInterval</c> (originally ported for
/// PayrollManagement's <c>EmployeeSubGroupPayment</c>) -- Wave 2's <see cref="EmployeeBasicPay"/> initially
/// declared a duplicate PersonnelAdministration-scoped copy of this same enum, which was consolidated away
/// after it caused a Swagger/NSwag schema-id collision (both were named "PayInterval").
///
/// DELIBERATE SIMPLIFICATIONS: the legacy auto-EndDate-chaining/overlap-adjustment logic (based on
/// <c>SysService.GetPayrollSettings(...).EnableEffectiveDateInRecPayment</c>), the retro-calculation
/// <c>SetRetroDate</c> cascade, and the <c>IsPayrollSensitiveClass</c>/<c>IsSystemGenerated</c>
/// earliest-master-data-change bookkeeping are Payroll-engine concerns not yet ported -- only the essential
/// no-duplicate/overlap-per-employee-and-wagetype validation is ported (see
/// <c>EmployeeRecurringPaymentsAppService</c>), same cross-record-cascade omission approach as every other
/// ledger entity in this port.
/// </summary>
public class EmployeeRecurringPayment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual PayInterval PayInterval { get; set; }
}
