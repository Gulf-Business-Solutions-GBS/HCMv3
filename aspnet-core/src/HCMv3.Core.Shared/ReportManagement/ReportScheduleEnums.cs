namespace HCMv3.ReportManagement;

/// <summary>Which report family a <c>ReportSchedule</c> targets.</summary>
public enum ScheduledReportKind
{
    /// <summary>One of the ported analysis grid reports, identified by its app-service report key.</summary>
    GridReport = 0,

    /// <summary>One of the ported DevExpress print reports, identified by <c>PrintReport.ReportName</c>.</summary>
    PrintReport = 1,
}

/// <summary>Output format a schedule renders to.</summary>
public enum ScheduledReportFormat
{
    Excel = 0,
    Pdf = 1,
    Csv = 2,
}

/// <summary>
/// The dimension a schedule is BURST by -- i.e. what one output file corresponds to.
///
/// <see cref="None"/> produces a single file for the whole (RLS-filtered) result. Every other value
/// produces one file per distinct key value, delivered only to that key's resolved recipients.
/// </summary>
public enum ReportBurstDimension
{
    /// <summary>No bursting: one file, one recipient set.</summary>
    None = 0,

    /// <summary>One file per organization unit (department/division/section, per the chosen level).</summary>
    OrganizationUnit = 1,

    /// <summary>One file per line manager, containing that manager's own subtree.</summary>
    Manager = 2,

    /// <summary>One file per sponsor.</summary>
    Sponsor = 3,

    /// <summary>One file per payroll area.</summary>
    PayrollArea = 4,
}

/// <summary>
/// How a <c>ReportScheduleRecipient</c> row resolves to actual people at run time.
///
/// These are RULES, not addresses: they are evaluated per burst slice when the job runs, so a schedule
/// created today keeps working after staff move between departments or change roles.
/// </summary>
public enum ReportRecipientRule
{
    /// <summary>
    /// Everyone holding a given role, optionally scoped to the slice's org unit. Resolved through
    /// <c>SysUserOrgUnitData</c> (Employee x Role x OrgLevel), which is what makes
    /// "the Department Secretary OF THIS DEPARTMENT" expressible rather than "every secretary".
    /// Covers HR Team, Payroll Team, Finance Team, Time Admins and Department Secretary.
    /// </summary>
    RoleScoped = 0,

    /// <summary>The head of the slice's organization unit.</summary>
    OrgUnitHead = 1,

    /// <summary>The line manager the slice belongs to (only meaningful with <see cref="ReportBurstDimension.Manager"/>).</summary>
    Manager = 2,

    /// <summary>A literal e-mail address. The escape hatch for recipients who are not HCMv3 users at all.</summary>
    Explicit = 3,
}

/// <summary>Where a rendered slice is delivered.</summary>
public enum ReportDestinationMethod
{
    /// <summary>E-mailed to the resolved recipients of that slice.</summary>
    Email = 0,

    /// <summary>Pushed to an FTP/SFTP server.</summary>
    Ftp = 1,

    /// <summary>Written to the configured storage and left for collection; no push.</summary>
    Offline = 2,
}

/// <summary>How often a schedule fires.</summary>
public enum ReportScheduleOccurrenceType
{
    /// <summary>Runs once, at <c>FirstOccurrence</c>.</summary>
    Once = 0,

    /// <summary>Runs repeatedly on <c>RecurringPattern</c> until <c>EndingOn</c>.</summary>
    Recurring = 1,
}

/// <summary>Recurring cadence. Deliberately a small fixed set rather than a raw cron expression.</summary>
public enum ReportRecurringPattern
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4,
}

/// <summary>Outcome of a whole schedule run, or of one slice within it.</summary>
public enum ReportRunStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,

    /// <summary>At least one slice failed while others succeeded.</summary>
    PartiallySucceeded = 3,

    Failed = 4,

    /// <summary>
    /// The slice produced no rows once the recipient's row-level-security scope was applied. Recorded
    /// distinctly from Succeeded so an empty inbox is explainable without re-running anything.
    /// </summary>
    SkippedEmpty = 5,
}
