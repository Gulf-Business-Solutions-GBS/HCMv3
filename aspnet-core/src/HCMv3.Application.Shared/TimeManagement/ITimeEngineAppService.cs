using System.Threading.Tasks;
using Abp.Application.Services;
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
}
