using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee non-payroll transaction record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeNonPayrollTrans</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="WageTypeId"/> FKs <c>HCMv3.PayrollManagement.WageType</c> (already ported), restricted in
/// legacy to non-leave/overtime/loan/ticket/violation earning-or-deduction wage types via the
/// <c>NonPayrollWageType</c> data-source filter -- NOT ported as a DB-level constraint (application-layer
/// concern, if enforced at all, belongs in <c>EmployeeNonPayrollTransAppService</c>).
/// <see cref="CurrencyId"/> FKs <c>HCMv3.GeneralMasterInfo.Currency</c>.
/// </summary>
public class EmployeeNonPayrollTrans : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Amount { get; set; }

    public virtual int? CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual bool IsLocked { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual bool NotificationSent { get; set; }

    [StringLength(256)]
    public virtual string PayableBy { get; set; }

    [StringLength(256)]
    public virtual string PaymentMethod { get; set; }
}
