namespace HCMv3.PayrollManagement;

/// <summary>
/// Payroll run type (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollRunType</c>), referenced by
/// <see cref="CostRegister"/>/<see cref="SalaryRegister"/>/<see cref="SimulationSalaryRegister"/>. Did not
/// exist yet from any earlier Payroll wave (confirmed via full-solution search) — created fresh in this
/// wave.
/// </summary>
public enum PayrollRunType
{
    Regular = 0,
    OffCycle = 1
}
