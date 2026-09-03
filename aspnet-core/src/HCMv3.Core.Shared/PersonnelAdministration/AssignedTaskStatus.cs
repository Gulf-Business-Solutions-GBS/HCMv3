namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Where an assigned piece of work has got to (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.TaskStatus</c>, declared on the legacy
/// <c>TaskRequest</c> business object).
///
/// <para>
/// The legacy NUMBERS are kept exactly as they were, odd ordering and all -- Closed is 1 and Working is 2,
/// which is not the order the work happens in. Data migrates on these numbers, so renumbering them into a
/// tidy lifecycle order would silently reclassify every historical row: every closed task would come across
/// as in progress.
/// </para>
///
/// <para>
/// Deliberately NOT called <c>TaskStatus</c>, which is what legacy called it. Two things in this codebase
/// already own that word -- <c>HCMv3.SystemSecurity.TaskEvent</c> and the <c>int TaskStatus</c> column on
/// <see cref="EmployeeTask"/> (a different concept entirely; see <c>EmployeeAssignedTask</c>) -- so a bare
/// <c>TaskStatus</c> here would either collide outright or, worse, compile and be read as the other one.
/// </para>
/// </summary>
public enum AssignedTaskStatus
{
    /// <summary>Assigned, not started.</summary>
    Opened = 0,

    /// <summary>Finished.</summary>
    Closed = 1,

    /// <summary>In progress.</summary>
    Working = 2,

    /// <summary>Started but parked, waiting on something else.</summary>
    OnHold = 3
}
