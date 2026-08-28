using System.Collections.Generic;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// One burst slice: a key the report is split by, plus the people entitled to receive that key's output.
/// </summary>
public class ReportBurstSlice
{
    /// <summary>
    /// Stable identifier of the slice -- an org unit id, manager employee id, sponsor id, and so on.
    /// Empty string for an unburst schedule, which has exactly one slice covering everything.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable name, captured now so run history stays legible after renames.</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>
    /// The organization unit this slice corresponds to, when bursting by org unit. Needed so
    /// role-scoped recipients can be matched WITHIN the slice ("this department's secretary").
    /// </summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>The manager this slice corresponds to, when bursting by manager.</summary>
    public int? ManagerEmployeeId { get; set; }

    /// <summary>
    /// The employees this slice covers -- the burst key expressed as a set.
    ///
    /// Null for an unburst schedule, meaning "the slice imposes no restriction of its own"; the
    /// recipient's scope alone decides. For a burst slice this is what makes the department-wise case
    /// work: the effective scope handed to the report is this set INTERSECTED with the recipient's own
    /// entitlement, so a head receives their department's rows and only the ones they may see.
    ///
    /// Expressing the burst key as employees rather than as a report parameter is what keeps every one
    /// of the ~138 grid reports usable without modification: they already filter on an employee id set.
    /// </summary>
    public HashSet<int> EmployeeIds { get; set; }

    public List<ReportSliceRecipient> Recipients { get; set; } = new List<ReportSliceRecipient>();
}

/// <summary>
/// A resolved recipient of one slice, together with the decision about whether their copy is scoped.
/// </summary>
public class ReportSliceRecipient
{
    public long? UserId { get; set; }

    public int? EmployeeId { get; set; }

    public string Email { get; set; }

    public string DisplayName { get; set; }

    /// <summary>
    /// False only for an <see cref="ReportRecipientRule.Explicit"/> recipient whose rule sets
    /// <c>BypassRecipientScope</c>. Carried per recipient rather than per slice because one slice can
    /// legitimately mix scoped internal recipients with an unscoped external auditor, and the run log
    /// has to record which copy was which.
    /// </summary>
    public bool ApplyRecipientScope { get; set; } = true;

    /// <summary>Which rule produced this recipient. Recorded for traceability in the run log.</summary>
    public ReportRecipientRule ResolvedByRule { get; set; }
}
