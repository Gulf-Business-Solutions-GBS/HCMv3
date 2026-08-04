using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.EngineRuns.Dto;
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
/// </summary>
public interface ITimeEngineAppService : IApplicationService
{
    /// <summary>
    /// Queues a Time Engine run and returns immediately. Never runs the engine inline: a run covers every
    /// employee x every day in the window and routinely takes minutes to hours.
    /// </summary>
    Task<RunTimeEngineOutput> RunTimeEngine(RunTimeEngineInput input);

    /// <summary>
    /// Live progress for the latest Time Engine run. Poll this, exactly as
    /// <c>project-cost-engine.component.ts</c> polls <c>GetProjectCostEngineStatus</c> (a port of legacy's
    /// <c>CalculateProcessStatus</c> timer).
    ///
    /// <para>Genuinely live: the engine publishes progress on its own committed transactions rather than
    /// inside the run's single outer unit of work, so these figures move while the run is still open.</para>
    /// </summary>
    Task<EngineRunStatusDto> GetTimeEngineStatus();

    /// <summary>
    /// The per-employee grid -- every employee the run processed, succeeded and failed, with the error
    /// message on failures. Leave <c>RunId</c> null to follow the latest run.
    /// </summary>
    Task<PagedResultDto<EngineRunEmployeeDto>> GetTimeEngineRunEmployees(GetEngineRunEmployeesInput input);

    /// <summary>Asks the in-flight run to stop after the employee it is currently processing.</summary>
    Task StopTimeEngine();

    /// <summary>Clears finished Time Engine progress history. The run in flight, if any, is left alone.</summary>
    Task ClearTimeEngineProgress();

    /// <summary>
    /// Ages out runs that stopped heart-beating (process killed / crashed / host recycled) so nothing is
    /// left marked "in progress" forever. Also happens automatically when the next run starts.
    /// </summary>
    Task<int> AbandonStaleTimeEngineRuns();
}
