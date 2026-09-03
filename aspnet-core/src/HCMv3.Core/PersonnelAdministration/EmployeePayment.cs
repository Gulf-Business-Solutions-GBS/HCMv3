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
/// the entity itself. The legacy <c>DocAttachment</c> FK is dropped.
///
/// WAVE P8: <see cref="OffCyclePaymentDate"/> and <see cref="IsSystemGenerated"/> were originally dropped as
/// "internal-only"; they are NOT -- they carry the payroll engine's advance-payment (wage code 2005) upsert
/// key and its engine-written marker. Both restored.
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

    /// <summary>
    /// Legacy <c>EmployeePayment.OffCyclePaymentDate</c>. RESTORED (wave P8): it is part of legacy's
    /// advance-payment upsert key <c>(Employee, PaymentDate, OffCyclePaymentDate, WageType)</c> in
    /// <c>Program.AdvancePaymentDed</c> (legacy <c>Program.cs</c> line 2940). Matching without it merges
    /// recovery rows belonging to different off-cycle runs and over-deducts. Nullable here (legacy used
    /// <c>default(DateTime)</c> for "regular run"); <see cref="PayrollManagement.Services.IPayrollAdvancePaymentService"/>
    /// normalises <c>default(DateTime)</c> to <c>null</c> so both regular and off-cycle runs key consistently.
    /// </summary>
    public virtual DateTime? OffCyclePaymentDate { get; set; }

    /// <summary>
    /// The <see cref="EmployeePaymentBatch"/> this payment was created from, when it came from one. A bare
    /// FK with no navigation property, exactly like <c>TimeRegister.TimeLogApprovalId</c>.
    ///
    /// <para>
    /// It is not decoration -- it is the idempotency key.
    /// <c>PaymentBatchWorkflowTransactionHandler.OnApprovedAsync</c> refuses to write a second time for a
    /// batch that already has payments against it, and this column is how it knows. Legacy had no such link
    /// and created the payments from <c>PaymentRequest.OnSaving</c>, so every re-save of an approved batch
    /// duplicated the lot.
    /// </para>
    ///
    /// <para>
    /// Null for a payment keyed in directly on the <c>EmployeePayment</c> screen or written by the payroll
    /// engine, which is the great majority of them.
    /// </para>
    /// </summary>
    public virtual int? EmployeePaymentBatchId { get; set; }

    /// <summary>
    /// Legacy <c>EmployeePayment.IsSystemGenerated</c> (set to <c>true</c> at legacy line 2983). BUSINESS
    /// data -- "written by the payroll engine" vs "keyed in by a user" -- not audit metadata that
    /// <c>FullAuditedEntity</c> already covers.
    /// </summary>
    public virtual bool IsSystemGenerated { get; set; }
}
