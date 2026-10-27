using System;
using System.Threading;
using Abp;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Marks the async flow of a SIMULATION (test) payroll run -- legacy's "Simulation" check box on the Payroll
/// Engine screen, which ran <c>HCM10.PayrollEngine/SimulationPayrollHelper.Main</c> instead of
/// <c>Program.Main</c>.
///
/// <para><b>Why an ambient flag and not only <see cref="PayrollRegisterRef.Target"/>.</b> The wage rules that
/// receive a <see cref="PayrollRegisterRef"/> read its <see cref="PayrollRegisterRef.Target"/>. But several
/// services the engine reaches take a bare register id (<c>IPayrollDayCountService</c>,
/// <c>IIndemnityCalculationService</c>, the wage-type exception gate) or write data that is not a register line
/// at all (loan recoveries, fund and insurance balances, the 2005/2011 carry-forward payments, the indemnity
/// quota, the leave's "paid in advance" mark). Legacy's simulation clone wrote NONE of those: every one of its
/// <c>*_Simu</c> methods has the write commented out. This flag is how those services know, without changing
/// every signature they expose.</para>
///
/// <para>Opened by <see cref="PayrollEngineService"/> for the whole of a simulation run and closed when it
/// ends. Same <see cref="AsyncLocal{T}"/> depth-counter idiom as
/// <c>PayrollRetroTrigger.SuppressEarliestMasterDataChange</c>.</para>
/// </summary>
public static class PayrollSimulationScope
{
    private static readonly AsyncLocal<int> Depth = new();

    /// <summary>True while a simulation run is being calculated on this async flow.</summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>Opens a simulation scope; dispose it to close. Scopes nest.</summary>
    public static IDisposable Begin()
    {
        Depth.Value++;
        return new DisposeAction(() => Depth.Value--);
    }
}
