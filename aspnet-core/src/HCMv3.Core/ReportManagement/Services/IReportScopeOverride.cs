using System;
using System.Collections.Generic;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Ambient "run this report AS somebody else" scope, set by the report-distribution executor for the
/// duration of one slice.
///
/// WHY THIS EXISTS -- AND WHY IT IS A SECURITY CONTROL, NOT A CONVENIENCE.
///
/// Every grid report resolves its row-level security through
/// <c>GridReportAppServiceBase.GetRlsEmployeeIdsAsync()</c>, which reads <c>AbpSession</c>. Two things
/// make that unusable for scheduled distribution:
///
///   1. A scheduled run has NO SESSION. AbpSession.UserId is null, so the RLS resolution finds no
///      current user and no current employee, and returns an EMPTY list.
///   2. An empty list is this codebase's convention for "NO RESTRICTION" -- see the remarks on
///      GetRlsEmployeeIdsAsync and the legacy `if (empList.Count != 0)` it reproduces.
///
/// Together those mean an unmodified grid report, run from the scheduler, returns EVERY EMPLOYEE IN THE
/// TENANT -- and does so silently, because the file looks entirely normal, just far too complete.
/// Mailing that to forty department heads is precisely the failure this module exists to prevent.
///
/// So the executor publishes the RECIPIENT's scope here before rendering, and the grid-report base
/// consults it ahead of the session. Putting it on an ambient accessor rather than changing ~100 report
/// signatures means every existing report inherits the correct behaviour without being edited.
/// </summary>
public interface IReportScopeOverride
{
    /// <summary>True while a slice is being rendered on behalf of a specific recipient.</summary>
    bool IsActive { get; }

    /// <summary>
    /// The recipient's visible employee ids while <see cref="IsActive"/>.
    ///
    /// Null means genuinely unrestricted (row-level security is off tenant-wide, or the recipient was
    /// explicitly exempted). An EMPTY set means the recipient is entitled to nothing and must receive
    /// nothing -- the exact opposite of the session-based convention, which is why
    /// <see cref="IsActive"/> has to exist rather than reusing null/empty alone.
    /// </summary>
    HashSet<int> VisibleEmployeeIds { get; }

    /// <summary>Begins an override; dispose the handle to restore the previous state.</summary>
    IDisposable Begin(HashSet<int> visibleEmployeeIds);
}
