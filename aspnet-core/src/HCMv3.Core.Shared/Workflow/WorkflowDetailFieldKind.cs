namespace HCMv3.Workflow;

/// <summary>
/// How a projected transaction field should be rendered, so one generic approver screen can format thirty
/// transaction types without knowing any of them.
///
/// <para>
/// The alternative -- returning an already-formatted string -- was rejected because formatting is the
/// client's job: the browser knows the viewer's locale, calendar and direction, and the server does not.
/// A date arrives as ISO-8601 and a number arrives invariant; the kind says which.
/// </para>
///
/// <para>
/// Lives in Core.Shared rather than Core because the DTO in Application.Shared carries it to the client,
/// and Application.Shared cannot reference Core.
/// </para>
/// </summary>
public enum WorkflowDetailFieldKind
{
    /// <summary>Plain text, shown exactly as given. The safe default for anything already resolved to a
    /// display name -- a lookup, an enum, a person.</summary>
    Text = 0,

    /// <summary>A calendar date with no time part. Value is ISO-8601 ("2026-09-23").</summary>
    Date = 1,

    /// <summary>A moment in time. Value is ISO-8601 round-trip ("2026-09-23T14:05:00").</summary>
    DateTime = 2,

    /// <summary>A count or quantity. Value is invariant-culture numeric ("2.5").</summary>
    Number = 3,

    /// <summary>An amount of money. Value is invariant-culture numeric; the client adds the currency.</summary>
    Money = 4,

    /// <summary>Yes/no. Value is "true" or "false", lower case.</summary>
    Boolean = 5,

    /// <summary>Free text long enough to deserve its own row rather than a column -- a reason, a
    /// justification, an address.</summary>
    LongText = 6
}
