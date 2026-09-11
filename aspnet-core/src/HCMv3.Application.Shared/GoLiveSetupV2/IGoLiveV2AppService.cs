using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.GoLiveSetupV2.Dto;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Go-Live Setup V2, built backwards from its goal. It starts from the one thing a customer actually wants --
/// a payroll that runs -- and lets the payroll engine say what is missing. Each refusal becomes the next
/// setup step.
///
/// <para>Deliberately separate from Go-Live Setup V1, which stays as it is so the two approaches can be
/// compared side by side.</para>
/// </summary>
public interface IGoLiveV2AppService : IApplicationService
{
    /// <summary>
    /// Runs the real payroll engine now, not on the background queue, and reports how far it got.
    ///
    /// <para>A refusal is the expected answer here, not an error, so it comes back as a result instead of
    /// being thrown -- the screen's whole job is to show it. Every input is optional for the same reason:
    /// the first press sends nothing, and the engine's reply says what to add.</para>
    ///
    /// <para>Inline execution holds the request open for the whole run, which suits a go-live test tenant
    /// with a handful of employees. The production runner remains the queued
    /// <c>IPayrollEngineAppService.RunPayrollEngine</c>.</para>
    /// </summary>
    Task<GoLiveV2RunPayrollOutput> RunPayroll(GoLiveV2RunPayrollInput input);

    /// <summary>
    /// Answers "Payroll Control Record not found": makes the payroll area the run named, if the tenant does not
    /// have it, and its control record for the month, locked as the engine requires for a regular run. An area
    /// that already exists is used as it is, and one that already has a control record keeps it.
    ///
    /// <para>A new area gets Kuwait defaults -- Kuwaiti Dinar, a January to December pay year, cut-off at month
    /// end -- and the result names them. Both records go through their own services, so every check those
    /// screens make applies here too.</para>
    /// </summary>
    Task<GoLiveV2SetUpPayrollMonthOutput> SetUpPayrollMonth(GoLiveV2SetUpPayrollMonthInput input);
}
