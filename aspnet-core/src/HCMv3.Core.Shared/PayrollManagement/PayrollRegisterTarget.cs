namespace HCMv3.PayrollManagement;

/// <summary>
/// Which register family a payroll run writes its results to.
///
/// <para>This enum exists ONLY because the legacy PayrollEngine solved the same problem by
/// copy-pasting: <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c> is a ~300 KB clone of
/// <c>Program.cs</c> carrying an identical static-field list, differing solely in that it writes
/// <c>SimulationSalaryRegister</c>/<c>SimulationSalaryRegisterDetail</c>/
/// <c>SimulationSalaryRegisterDaysDetail</c> rows instead of the live
/// <c>SalaryRegister</c>/<c>SalaryRegisterDetail</c>/<c>SalaryRegisterDaysDetail</c> ones. The two
/// entity families are structurally identical (same Employee/ForPeriod/InPeriod/PayrollRunType/
/// PayrollResultType/PayrollReasonType header shape and same WageType/Rate/Number/Amount line shape;
/// the only difference is that <c>SimulationSalaryRegister</c> has no <c>Locked</c> column).</para>
///
/// <para>Wave P1 therefore threads this value through <c>PayrollEngineRunInput</c> and
/// <c>PayrollCalculationContext</c> and funnels every register read/write through
/// <c>IPayrollRegisterStore</c>, so that collapsing the legacy clone (planned wave P7) means adding one
/// branch inside that single store implementation -- NOT re-writing the engine or any of the ~34 wage
/// rules of waves P4-P6.</para>
///
/// <para>Wave P1 implements <see cref="Live"/> only; <see cref="Simulation"/> is declared here so the
/// contract P4-P6 codes against is already final, and the store throws a loud
/// <c>NotImplementedException</c> for it rather than silently writing to the live tables.</para>
/// </summary>
public enum PayrollRegisterTarget
{
    /// <summary>Legacy <c>HCM10.PayrollEngine/Program.cs</c> -- writes the live <c>SalaryRegister</c> family.</summary>
    Live = 0,

    /// <summary>
    /// Legacy <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c> -- writes the
    /// <c>SimulationSalaryRegister</c> family. NOT implemented in wave P1 (see wave P7).
    /// </summary>
    Simulation = 1
}
