using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.Licensing;

/// <summary>
/// Gives and takes away per-user module licences, honouring each module's seat count (an edition feature). The only
/// writer of <see cref="UserModuleLicence"/>, so it is also what keeps the licence cache honest.
/// </summary>
public interface IUserModuleLicenceManager
{
    /// <summary>Seats in use and seats available for every module, for the current tenant.</summary>
    Task<List<LicenceModuleUsage>> GetUsageAsync();

    Task<LicenceModuleUsage> GetUsageAsync(LicenceModule module);

    /// <summary>
    /// Refused when the module is switched off for the tenant, the user is an administrator (never needs one), or no
    /// seat is left. Assigning a module the user already holds does nothing.
    /// </summary>
    Task AssignAsync(long userId, LicenceModule module);

    /// <summary>Unassigning a module the user does not hold does nothing.</summary>
    Task UnassignAsync(long userId, LicenceModule module);

    /// <summary>
    /// Legacy assigned ESS and Mobile licences automatically when it created an employee's user, when seats were
    /// free. Called when a user is linked to an employee. Never throws for a full module or one switched off -- the
    /// link itself must still succeed; it just skips that module. Returns the modules it assigned.
    /// </summary>
    Task<List<LicenceModule>> AssignOnEmployeeLinkAsync(long userId);
}
