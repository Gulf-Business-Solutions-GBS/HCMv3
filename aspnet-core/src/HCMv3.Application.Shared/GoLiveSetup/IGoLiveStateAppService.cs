using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Persists the Go-Live Setup draft so a customer can fill it over several days, and so two people can
/// own different steps without losing each other's work.
///
/// <para>Deliberately dumb storage: the payload is opaque JSON owned by the client. It becomes real
/// entities only when Apply runs, which is where validation and the typed schema belong.</para>
/// </summary>
public interface IGoLiveStateAppService : IApplicationService
{
    /// <summary>Everything saved for the current tenant, for hydrating the shell on entry.</summary>
    Task<GetGoLiveStateOutput> GetState();

    /// <summary>Upserts one step. Called on a debounce as the customer types.</summary>
    Task SaveStep(SaveGoLiveStepInput input);

    /// <summary>Discards the whole draft for the current tenant.</summary>
    Task ClearState();
}
