using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage-type calculation line of a <see cref="SimulationSalaryRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SimulationSalaryRegisterDetail</c>), linked back via
/// <see cref="SimulationSalaryRegisterId"/>. Its child is <see cref="SimulationSalaryRegisterDaysDetail"/>,
/// linked back via <see cref="SimulationSalaryRegisterDaysDetail.SimulationSalaryRegisterDetailId"/>. No
/// lock guard (see <see cref="SimulationSalaryRegister"/> remarks) — plain CRUD.
/// </summary>
public class SimulationSalaryRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SimulationSalaryRegisterId { get; set; }

    public virtual SimulationSalaryRegister SimulationSalaryRegister { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual double Rate { get; set; }

    public virtual double Number { get; set; }

    public virtual double Amount { get; set; }
}
