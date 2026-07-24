using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee one-off payment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeePayment</c>, Wave 4 deferred bucket).
///
/// <see cref="WageTypeId"/> FKs <c>HCMv3.PayrollManagement.WageType</c> (already ported).
/// <see cref="CurrencyId"/> FKs <c>HCMv3.GeneralMasterInfo.Currency</c>. <see cref="CostCenterId"/> FKs
/// <c>HCMv3.OrganizationManagement.CostCenter</c> (already ported).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>ExpectedAmount</c>/<c>GetExpectedAmount</c> computed-amount
/// logic depends on <c>EmployeeBasicPayDetail</c>/payroll-settings wage-rate calculation -- NOT ported on
/// the entity itself. The legacy <c>DocAttachment</c> FK and the internal-only <c>OffCyclePaymentDate</c>
/// field (hidden from every legacy view) are dropped.
/// </summary>
public class EmployeePayment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    /// <summary>Legacy display name "Number".</summary>
    public virtual double Amount { get; set; }

    public virtual int? CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual UnitType UnitType { get; set; }

    public virtual int? CostCenterId { get; set; }

    public virtual CostCenter CostCenter { get; set; }
}
