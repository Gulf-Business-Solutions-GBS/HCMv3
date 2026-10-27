using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Costing;

/// <summary>
/// Lets the caller of <see cref="ICostEngineService"/> see a run while it is in flight, as legacy's
/// status panel did.
///
/// <para>Legacy wrote one <c>ProjectLog</c> row per employee INSIDE the loop and committed it straight away
/// (<c>uscCostingFile.ascx.cs</c> lines 604-628 for success, 630-654 for failure), each employee on its own
/// object space. The page's timer then counted those rows for the progress ring and the Show-details grid,
/// and the Stop button flagged one of them. This observer is how the engine hands each finished employee
/// out at the moment it finishes, so the background job can commit it and write the same row; the engine
/// itself stays free of persistence it does not own.</para>
/// </summary>
public interface ICostingRunObserver
{
    /// <summary>
    /// Asked before every employee, after legacy's own <c>ProjectLog.StopFlag</c> check (legacy line 469).
    /// <c>true</c> ends the run there; employees already done stay done.
    /// </summary>
    Task<bool> IsStopRequestedAsync();

    /// <summary>
    /// One employee is finished, successfully or not. Called after the employee's cost-register work, so an
    /// implementation that saves here commits that employee on its own, as legacy did.
    /// </summary>
    /// <param name="entry">The employee's outcome, exactly as it goes into the run result.</param>
    /// <param name="totalEmployees">Legacy <c>NoofEmp</c>: how many employees the run selected.</param>
    Task OnEmployeeCompletedAsync(CostingRunLogEntry entry, int totalEmployees);
}
