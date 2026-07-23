using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// "What-if" trial payroll run header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SimulationSalaryRegister</c>). Structurally
/// near-identical to <see cref="SalaryRegister"/> (same EmployeeId/ForPeriod/InPeriod/PayrollRunType/
/// PayrollResultType/PayrollReasonTypeId shape) but CONFIRMED from the legacy source to have NO
/// <c>Locked</c> field and NO lock-cascade guard logic anywhere in the Simulation family — ported as plain
/// CRUD.
///
/// Ported as a genuinely separate entity family (not consolidated with <see cref="SalaryRegister"/> via an
/// <c>IsSimulation</c> flag) per this wave's explicit instruction for a faithful 1:1 port. NOTE/OPPORTUNITY:
/// a future refactor could merge this sandbox family into <see cref="SalaryRegister"/>/
/// <see cref="SalaryRegisterDetail"/>/<see cref="SalaryRegisterDaysDetail"/> with a single boolean flag,
/// eliminating three duplicate tables — deliberately NOT implemented in this wave. Its child is
/// <see cref="SimulationSalaryRegisterDetail"/>, linked back via
/// <see cref="SimulationSalaryRegisterDetail.SimulationSalaryRegisterId"/>.
/// </summary>
public class SimulationSalaryRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime ForPeriod { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual PayrollRunType PayrollRunType { get; set; }

    public virtual PayrollResultType PayrollResultType { get; set; }

    public virtual int PayrollReasonTypeId { get; set; }

    public virtual PayrollReasonType PayrollReasonType { get; set; }
}
