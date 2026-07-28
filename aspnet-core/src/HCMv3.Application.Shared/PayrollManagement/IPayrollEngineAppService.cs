using System.Threading.Tasks;
using Abp.Application.Services;
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
}
