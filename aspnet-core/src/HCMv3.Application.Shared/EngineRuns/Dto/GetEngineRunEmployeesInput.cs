using Abp.Application.Services.Dto;

namespace HCMv3.EngineRuns.Dto;

/// <summary>
/// Input for the per-employee progress grid.
///
/// <para>App-service methods taking this MUST carry <c>[HttpPost]</c>: ABP maps <c>Get*</c> to HTTP GET, and
/// NSwag would then flatten this class into query-string parameters and never emit it into
/// <c>service-proxies.ts</c>.</para>
/// </summary>
public class GetEngineRunEmployeesInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Which run to read. Leave <c>null</c> to follow the LATEST run of the engine -- which is what a live
    /// progress screen wants, since it polls before the run id is known.
    /// </summary>
    public long? RunId { get; set; }

    /// <summary>Free-text filter over employee code, employee name, and the message.</summary>
    public string Filter { get; set; }

    /// <summary>
    /// <c>true</c> = failures only, <c>false</c> = successes only, <c>null</c> = every employee. Operators
    /// almost always want <c>true</c> once a run finishes.
    /// </summary>
    public bool? OnlyFailed { get; set; }
}
