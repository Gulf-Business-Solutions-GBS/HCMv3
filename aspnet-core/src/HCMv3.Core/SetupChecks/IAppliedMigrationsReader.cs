using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.SetupChecks;

/// <summary>
/// Which database changes (EF migrations) this server's database has had applied. Tenant Version Check uses it to say
/// whether an update's database changes really arrived -- the one setup item that fails loudly at run time if missed.
/// </summary>
public interface IAppliedMigrationsReader
{
    /// <summary>Migration ids as EF records them, e.g. <c>20260919184248_Removed_GoLiveSetup_V1</c>. Null when the
    /// database keeps no migration history, so nothing can be said either way.</summary>
    Task<IReadOnlyCollection<string>> GetAppliedAsync();
}
