using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.EngineRuns.Dto;
using HCMv3.SystemSecurity.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

/// <summary>
/// Execution surface for the Time Engine (ported from legacy's console entry point
/// <c>HCM10.TimeEngine/Program.Main</c> plus the screen that shelled out to it,
/// <c>HCM10.Web.UserControls.PayrollManagement.uscTimeEngine</c>).
///
/// <para>The calculation itself lives in
/// <c>HCMv3.TimeManagement.Services.ITimeEngineService.CreateTimeRegisterAsync</c> (a Core domain service).
/// This app service only authorises the request and hands it to a background job -- see
/// <see cref="RunTimeEngineOutput"/> for where progress is published.</para>
///
/// <para><b>Payroll area scope.</b> The status, per-employee grid and Stop follow the run of the payroll area the
/// screen has selected (blank = the latest run of any area). Time-engine runs a payroll run makes for itself (the
/// payroll engine's per-employee autorun) are never shown here, as legacy never showed them.</para>
/// </summary>
public interface ITimeEngineAppService : IApplicationService
{
    /// <summary>
    /// Queues a Time Engine run and returns immediately. Never runs the engine inline: a run covers every
    /// employee x every day in the window and routinely takes minutes to hours. Refuses a second run of a
    /// payroll area that is already running.
    /// </summary>
    Task<RunTimeEngineOutput> RunTimeEngine(RunTimeEngineInput input);

    /// <summary>
    /// Live progress for the latest Time Engine run of <paramref name="payrollAreaName"/> (null or blank = of any
    /// area). Poll this, exactly as <c>project-cost-engine.component.ts</c> polls
    /// <c>GetProjectCostEngineStatus</c> (a port of legacy's <c>CalculateProcessStatus</c> timer). A plain string
    /// argument, so it stays an HTTP GET.
    ///
    /// <para>Genuinely live: the engine publishes progress on its own committed transactions rather than
    /// inside the run's unit of work, so these figures move while the run is still open.</para>
    /// </summary>
    Task<EngineRunStatusDto> GetTimeEngineStatus(string payrollAreaName = null);

    /// <summary>
    /// The per-employee grid -- every employee the run processed, succeeded and failed, with the error
    /// message on failures (an employee with any failed day is failed). Leave <c>RunId</c> null to follow the
    /// latest run of <c>PayrollAreaName</c>.
    /// </summary>
    Task<PagedResultDto<EngineRunEmployeeDto>> GetTimeEngineRunEmployees(GetTimeEngineRunEmployeesInput input);

    /// <summary>One employee's days in one run, from the time log: day date, status and message.</summary>
    Task<List<TimeEngineRunDayDto>> GetTimeEngineRunDays(GetTimeEngineRunDaysInput input);

    /// <summary>
    /// Legacy <c>btnExportTimeLog_Click</c>: the run's log, one row per employee per day, as an Excel file named
    /// <c>TimeEngineExecutionLog_yyyyMMdd_HHmmss</c>.
    /// </summary>
    Task<FileDto> GetTimeEngineRunLogToExcel(GetTimeEngineRunLogToExcelInput input);

    /// <summary>Asks the in-flight run of <paramref name="payrollAreaName"/> (blank = the latest of any area) to stop after the employee in hand.</summary>
    Task StopTimeEngine(string payrollAreaName = null);

    /// <summary>Clears finished Time Engine progress history. The run in flight, if any, is left alone.</summary>
    Task ClearTimeEngineProgress();

    /// <summary>
    /// Ages out runs that stopped heart-beating (process killed / crashed / host recycled) so nothing is
    /// left marked "in progress" forever. Also happens automatically when the next run starts.
    /// </summary>
    Task<int> AbandonStaleTimeEngineRuns();

    /// <summary>
    /// The employee picker (legacy popupTimeEmployees): the payroll area's employees for the window, each
    /// Processed or Pending, filtered and paged, with the counts for the four filter buttons.
    /// </summary>
    Task<TimeEngineRunCandidatesOutput> GetTimeEngineRunCandidates(GetTimeEngineRunCandidatesInput input);

    /// <summary>The codes the picker's current filter and search show, on every page -- for Select all and OK.</summary>
    Task<List<string>> GetTimeEngineRunCandidateCodes(GetTimeEngineRunCandidatesInput input);

    /// <summary>
    /// The screen's run history: the Time Engine's execution log, without the per-employee runs the payroll
    /// engine makes for itself.
    /// </summary>
    Task<PagedResultDto<ExecutionLogDto>> GetTimeEngineRunHistory(GetTimeEngineRunHistoryInput input);
}
