using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Per-day breakdown of a <see cref="SimulationSalaryRegisterDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SimulationSalaryRegisterDaysDetail</c>), linked back
/// via <see cref="SimulationSalaryRegisterDetailId"/>. No lock guard (see
/// <see cref="SimulationSalaryRegister"/> remarks) — plain CRUD.
/// </summary>
public class SimulationSalaryRegisterDaysDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SimulationSalaryRegisterDetailId { get; set; }

    public virtual SimulationSalaryRegisterDetail SimulationSalaryRegisterDetail { get; set; }

    public virtual DateTime Period { get; set; }

    public virtual double Rate { get; set; }

    public virtual double Number { get; set; }

    public virtual double Amount { get; set; }
}
