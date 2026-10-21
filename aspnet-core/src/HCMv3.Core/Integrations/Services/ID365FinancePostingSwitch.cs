using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Integrations.Services;

/// <summary>
/// The Integration Center's on/off switch for Dynamics 365 Finance, as the posting path reads it.
///
/// <para>The card's switch existed but nothing that posts journals ever looked at it, so switching the
/// integration off in the Integration Center did not stop Post JV from sending journals to D365. This is
/// the one place that answer is worked out; the Post JV screen asks it before queuing a posting and the
/// posting client asks it again before it signs in.</para>
/// </summary>
public interface ID365FinancePostingSwitch : IDomainService
{
    /// <summary><c>true</c> when journals may be sent to Dynamics 365 Finance for the current tenant.</summary>
    Task<bool> IsPostingAllowedAsync();
}
