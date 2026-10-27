using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Execution surface for the Payroll Engine (ported from legacy's console entry point
/// <c>HCM10.PayrollEngine/Program.Main</c> plus the screen that shelled out to it,
/// <c>HCM10.Web.UserControls.PayrollManagement.uscPayrollEngine</c>).
///
/// <para>The calculation itself lives in
/// <c>HCMv3.PayrollManagement.Services.IPayrollEngineService.RunAsync</c> (a Core domain service). This app
/// service only validates and authorises the request and hands it to a background job -- see
/// <see cref="RunPayrollEngineOutput"/> for where progress is published.</para>
/// </summary>
public interface IPayrollEngineAppService : IApplicationService
{
    /// <summary>
    /// Queues a Payroll Engine run and returns immediately. Never runs the engine inline: a run covers every
    /// employee x every retro period x every wage type and routinely takes minutes to hours.
    /// </summary>
    Task<RunPayrollEngineOutput> RunPayrollEngine(RunPayrollEngineInput input);

    /// <summary>
    /// Live progress for the latest Payroll Engine run. Same contract, same DTO and same polling idiom as
    /// <c>ITimeEngineAppService.GetTimeEngineStatus</c> and <c>GetProjectCostEngineStatus</c> -- one UI
    /// component can serve all of them.
    /// </summary>
    Task<EngineRunStatusDto> GetPayrollEngineStatus();

    /// <summary>
    /// The per-employee grid -- every employee the run processed, succeeded and failed, with the error
    /// message on failures. Leave <c>RunId</c> null to follow the latest run.
    /// </summary>
    Task<PagedResultDto<EngineRunEmployeeDto>> GetPayrollEngineRunEmployees(GetEngineRunEmployeesInput input);

    /// <summary>Asks the in-flight run to stop after the employee it is currently processing.</summary>
    Task StopPayrollEngine();

    /// <summary>Clears finished Payroll Engine progress history. The run in flight, if any, is left alone.</summary>
    Task ClearPayrollEngineProgress();

    /// <summary>
    /// Ages out runs that stopped heart-beating (process killed / crashed / host recycled) so nothing is
    /// left marked "in progress" forever. Also happens automatically when the next run starts.
    /// </summary>
    Task<int> AbandonStalePayrollEngineRuns();
}
