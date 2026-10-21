using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>Which of the two finance screens a run belongs to.</summary>
public enum FinanceRunProcess
{
    /// <summary>Legacy <c>Proc_CostEngine</c> ("Cost Engine"): the Costing File screen's Generate button.</summary>
    CostEngine = 0,

    /// <summary>Legacy <c>Proc_PostJV</c> ("Post JV"): posting the JV to Dynamics 365.</summary>
    PostJv = 1
}

/// <summary>The run currently queued or in progress on either finance screen.</summary>
public class FinanceActiveRun
{
    public int ExecutionLogId { get; set; }

    public FinanceRunProcess Process { get; set; }

    public string StartedBy { get; set; }

    public DateTime StartedAt { get; set; }
}

/// <summary>
/// The log legacy's Costing File screen kept for both of its long operations, and the run record around it.
///
/// <para><b>The log is <c>ProjectLog</c>, exactly as legacy</b> (<c>uscCostingFile.ascx.cs</c>): one row per
/// employee for the cost engine (lines 604-654), one row per D365 journal header and per line batch for Post
/// JV (lines 1346, 1360, 1442, 1471), and a closing row per journal or per off-cycle employee. The table is
/// emptied when a run starts (legacy <c>TruncateLog</c>, line 2210) and by Clear Status. The status panel,
/// the progress ring and the "Show details" grid are all read from it.</para>
///
/// <para><b>The run record is <c>ExecutionLog</c></b>, which legacy opened for the cost engine (lines 397-424)
/// and closed with the aggregate counts (lines 660-690). It is opened for Post JV too, because a background job
/// needs something that says a run is queued, running or finished -- legacy used a static variable for that,
/// which does not survive a restart and does not work on more than one server.</para>
///
/// <para><b>Every write is saved at once.</b> The jobs run in a non-transactional unit of work, so a row is
/// committed and visible to the status panel the moment it is written, as legacy committed each object space;
/// nothing a run has already done, including what has already been sent to D365, is rolled back by a later
/// failure.</para>
///
/// <para>No method that a job calls throws on a log failure it can survive; the jobs themselves never
/// rethrow.</para>
/// </summary>
public interface IFinanceRunLog
{
    /// <summary>Opens the run's <c>ExecutionLog</c> row as INPRECESS with legacy's parameter string.</summary>
    Task<int> OpenAsync(FinanceRunProcess process, CostingRunInput input, string userName);

    /// <summary>
    /// Called by the job before it does anything. <c>false</c> when the row is not a fresh INPRECESS run --
    /// i.e. the job has already started once and is being executed again after a restart. A second Post JV
    /// would send the journals to D365 a second time, so the job must then stop.
    /// </summary>
    Task<bool> TryClaimAsync(int executionLogId);

    /// <summary>Closes a run whose job was found already started (see <see cref="TryClaimAsync"/>), with a log row saying so.</summary>
    Task MarkInterruptedAsync(int executionLogId, DateTime inPeriod);

    /// <summary>One <c>ProjectLog</c> row, saved at once.</summary>
    Task WriteAsync(int executionLogId, int? employeeId, DateTime inPeriod, string message, int status, int noOfEmployee);

    /// <summary>Legacy <c>ProjectLog.StopFlag</c> on any row, or a Stop pressed before any row existed.</summary>
    Task<bool> IsStopRequestedAsync(int executionLogId);

    /// <summary>Legacy's <c>finally</c> (lines 660-690): ABORTED / SUCCESS / FAILED / PARTIAL from the log counts.</summary>
    Task CloseAsync(int executionLogId);

    /// <summary>The run queued or in progress on either screen, or <c>null</c>. A run silent for too long does not count.</summary>
    Task<FinanceActiveRun> GetActiveRunAsync();

    /// <summary>Legacy Stop button (lines 2214-2225 / 2253-2264), plus a stop request kept for the running job.</summary>
    Task RequestStopAsync();

    /// <summary>Legacy Clear Status: empties the log.</summary>
    Task ClearAsync();

    /// <summary>Legacy <c>CalculateProcessStatus</c> for one screen.</summary>
    Task<CostingProcessStatusDto> GetStatusAsync(FinanceRunProcess process);

    /// <summary>Legacy "Show details" grid, one page.</summary>
    Task<FinanceRunLogOutput> GetLogAsync(GetFinanceRunLogInput input);

    /// <summary>Every row the grid's filter selects, for legacy's Export Log button.</summary>
    Task<List<FinanceRunLogDto>> GetLogRowsAsync(GetFinanceRunLogInput input);
}
