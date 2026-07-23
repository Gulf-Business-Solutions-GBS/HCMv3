using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Per-day breakdown of a <see cref="SalaryRegisterDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SalaryRegisterDaysDetail</c>), linked back via
/// <see cref="SalaryRegisterDetailId"/>.
///
/// LOCK CASCADE PARITY (enforced in <c>SalaryRegisterDaysDetailsAppService</c>, not here): create/update/
/// delete are blocked when the ultimate parent (<c>SalaryRegisterDetail.SalaryRegister</c>) is Locked.
/// </summary>
public class SalaryRegisterDaysDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SalaryRegisterDetailId { get; set; }

    public virtual SalaryRegisterDetail SalaryRegisterDetail { get; set; }

    public virtual DateTime Period { get; set; }

    public virtual double Rate { get; set; }

    public virtual double Number { get; set; }

    public virtual double Amount { get; set; }
}
