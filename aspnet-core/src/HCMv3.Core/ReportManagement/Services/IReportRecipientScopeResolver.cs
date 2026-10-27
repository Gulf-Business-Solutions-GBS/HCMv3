using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Resolves the set of Employee ids a GIVEN user is entitled to see -- the row-level-security scope of
/// somebody other than the caller.
///
/// WHY THIS EXISTS RATHER THAN REUSING IRlsService. <c>IRlsService.GetRlsEmployeeIdsAsync</c> answers
/// "what may the CURRENT SESSION USER see", reading <c>AbpSession.UserId</c> and the current employee.
/// That is the right shape for an interactive request and the wrong shape for report distribution: a
/// scheduled run has no session at all, and even a manually triggered run is executing as the schedule's
/// author, not as the recipient. Reusing the session-scoped call would filter every slice by the
/// author's entitlement -- typically an HR admin who can see the whole company -- and then mail it to
/// department heads. That is precisely the data leak this module has to prevent, so the parameterised
/// form is a separate, explicit contract.
///
/// The composition mirrors <c>RlsService.GetRlsEmployeeIdsAsync</c> exactly (manager subtree UNION
/// role-based org-unit subtree, plus the Level0 "sees unassigned employees" rule) so a recipient sees
/// the same rows they would see by logging in and opening the report themselves. If the two ever
/// diverge, the interactive view is the specification and this must be corrected to match.
/// </summary>
public interface IReportRecipientScopeResolver
{
    /// <summary>
    /// Employee ids visible to <paramref name="userId"/>.
    ///
    /// Returns null -- NOT an empty list -- when row-level security is switched off tenant-wide, meaning
    /// "no restriction". Empty means "this user is entitled to nothing", which is a legitimate and very
    /// different answer. Conflating the two would turn an RLS-disabled tenant into one that mails empty
    /// files to everybody, or worse, turn an unentitled recipient into one who receives everything.
    /// </summary>
    Task<HashSet<int>> GetVisibleEmployeeIdsForUserAsync(long userId);

    /// <summary>
    /// Same, for a recipient identified by employee rather than user. Used for recipients resolved from
    /// the org chart (a unit head, a line manager) who may not have a user account at all.
    /// </summary>
    Task<HashSet<int>> GetVisibleEmployeeIdsForEmployeeAsync(int employeeId);
}
