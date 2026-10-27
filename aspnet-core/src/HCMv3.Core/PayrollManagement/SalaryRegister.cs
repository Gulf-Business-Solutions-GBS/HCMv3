using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Calculated salary register header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SalaryRegister</c>). Its child is
/// <see cref="SalaryRegisterDetail"/>, linked back via <see cref="SalaryRegisterDetail.SalaryRegisterId"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy non-persistent, computed <c>RegisterInfo</c> display string
/// (built from Employee.Code + PayrollRunType + period formatting) is a pure UI convenience and is NOT
/// ported — it can be composed client-side from <see cref="Employee"/>/<see cref="PayrollRunType"/>/
/// <see cref="ForPeriod"/>/<see cref="InPeriod"/> if needed.
///
/// LOCK CASCADE PARITY (enforced in <c>SalaryRegistersAppService</c>, not here): the legacy's
/// session-cached <c>prevLocked</c>/<c>OnLoaded</c> guard — "a Locked register cannot be updated while
/// remaining Locked, and cannot be deleted while Locked" — is replicated per-request by loading the
/// persisted entity before applying changes and comparing its previous <see cref="Locked"/> value to the
/// incoming one.
/// </summary>
public class SalaryRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime ForPeriod { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual PayrollRunType PayrollRunType { get; set; }

    public virtual PayrollResultType PayrollResultType { get; set; }

    /// <summary>
    /// NULLABLE, matching legacy. The legacy XPO reference <c>SalaryRegister.PayrollReasonType</c> is an
    /// optional association and is NULL on the vast majority of production rows (62,400 of 64,999): a REGULAR
    /// payroll run never assigns a reason -- only off-cycle runs resolve one from
    /// <c>PayrollOffCycleReasons</c>. Porting this as a NOT NULL FK made it impossible to persist a regular
    /// register at all (<c>FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId</c> violation on
    /// SaveChanges).
    /// </summary>
    public virtual int? PayrollReasonTypeId { get; set; }

    public virtual PayrollReasonType PayrollReasonType { get; set; }

    public virtual DateTime OffCyclePaymentDate { get; set; }

    public virtual bool Locked { get; set; }
}
