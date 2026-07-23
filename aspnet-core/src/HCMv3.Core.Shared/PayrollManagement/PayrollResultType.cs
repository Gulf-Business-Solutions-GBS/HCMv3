namespace HCMv3.PayrollManagement;

/// <summary>
/// Payroll result type (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollResultType</c>), referenced by
/// <see cref="SalaryRegister"/>/<see cref="SimulationSalaryRegister"/>. Did not exist yet from any earlier
/// Payroll wave (confirmed via full-solution search) — created fresh in this wave.
/// </summary>
public enum PayrollResultType
{
    Current = 0,
    Last = 1,
    Archive = 2
}
