using System;
using System.Collections.Generic;
using System.Globalization;

namespace HCMv3.Workflow.Services;

/// <summary>
/// One label/value row of a transaction, as its own handler chooses to present it.
///
/// <para>
/// The label is already localized -- the handler holds the field names, and only the handler knows what its
/// columns mean. The value is a transport string whose format is fixed by <see cref="Kind"/>, so the client
/// can render a date in the viewer's calendar rather than in the server's.
/// </para>
/// </summary>
public class WorkflowDetailField
{
    public WorkflowDetailField(string name, string label, string value, WorkflowDetailFieldKind kind = WorkflowDetailFieldKind.Text)
    {
        Name = name;
        Label = label;
        Value = value;
        Kind = kind;
    }

    /// <summary>
    /// A stable key for this field -- the entity's own property name, e.g. <c>ExternalRequestNumber</c>.
    ///
    /// <para>
    /// The label is for reading and the name is for binding, and they have to be separate things. A client
    /// that wants to render this transaction on its OWN form -- the real create form, read-only, which is
    /// what the user asked for -- has to put each value in the right input, and it cannot do that by
    /// matching a label: the label is localized, so the match would break the moment the viewer switched to
    /// Arabic, and it is prose, so it would break the moment somebody improved the wording.
    /// </para>
    /// </summary>
    public string Name { get; }

    public string Label { get; }

    public string Value { get; }

    public WorkflowDetailFieldKind Kind { get; }

    /// <summary>A date field, or null when the date is unset -- an empty row is more honest than 01/01/0001.</summary>
    public static WorkflowDetailField Date(string name, string label, DateTime? value)
    {
        return new WorkflowDetailField(
            name,
            label,
            value.HasValue && value.Value != default ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            WorkflowDetailFieldKind.Date);
    }

    public static WorkflowDetailField DateTimeField(string name, string label, DateTime? value)
    {
        return new WorkflowDetailField(
            name,
            label,
            value.HasValue && value.Value != default ? value.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) : null,
            WorkflowDetailFieldKind.DateTime);
    }

    public static WorkflowDetailField Number(string name, string label, decimal? value)
    {
        return new WorkflowDetailField(
            name,
            label,
            value?.ToString(CultureInfo.InvariantCulture),
            WorkflowDetailFieldKind.Number);
    }

    public static WorkflowDetailField Money(string name, string label, decimal? value)
    {
        return new WorkflowDetailField(
            name,
            label,
            value?.ToString(CultureInfo.InvariantCulture),
            WorkflowDetailFieldKind.Money);
    }

    public static WorkflowDetailField Boolean(string name, string label, bool value)
    {
        return new WorkflowDetailField(name, label, value ? "true" : "false", WorkflowDetailFieldKind.Boolean);
    }

    public static WorkflowDetailField Text(string name, string label, string value)
    {
        return new WorkflowDetailField(name, label, value);
    }

    public static WorkflowDetailField LongText(string name, string label, string value)
    {
        return new WorkflowDetailField(name, label, value, WorkflowDetailFieldKind.LongText);
    }
}

/// <summary>
/// The field names the engine itself contributes, so a client can recognise them without string literals
/// scattered through it.
/// </summary>
public static class WorkflowDetailFieldNames
{
    public const string RequestNumber = "RequestNumber";

    public const string SubmittedOn = "SubmittedOn";

    public const string Summary = "Summary";
}

/// <summary>
/// Everything an approver needs to READ about a transaction, written by the type's own handler.
///
/// <para>
/// This is the second half of what <see cref="WorkflowSummary"/> started. The summary answers "which request
/// is this?" in one line, which is all an inbox list needs. It is not enough to decide with: the user's
/// complaint was exactly that an approver saw a sentence where the requester had filled in a form, and for
/// an exit permit the sentence happened to carry the permit number and the exit date but could never reach
/// the start date. A projection of label/value pairs answers "what am I approving?" for every type at once,
/// which is what stops this becoming thirty approval screens -- the shape legacy actually had.
/// </para>
///
/// <para>
/// Handlers build this on demand rather than storing it, unlike the summary. The summary is frozen at submit
/// because the inbox must not fan out across transaction tables to render a list; a detail view is already
/// looking at one request, so one more row read costs nothing and the approver sees the row as it stands.
/// </para>
/// </summary>
public class WorkflowRequestDetail
{
    public List<WorkflowDetailField> Fields { get; set; } = new List<WorkflowDetailField>();

    /// <summary>
    /// The note the requester typed when submitting, wherever their transaction row keeps it.
    ///
    /// <para>
    /// Surfaced here rather than left as one more field because the engine uses it too: it becomes the
    /// remark on the <c>Created</c> log entry, which is what makes the requester's own words the first post
    /// in the activity feed instead of an unexplained "Created" line. Before this it was written nowhere the
    /// feed could reach.
    /// </para>
    /// </summary>
    public string SubmissionComment { get; set; }

    public WorkflowRequestDetail Add(WorkflowDetailField field)
    {
        if (field != null)
        {
            Fields.Add(field);
        }

        return this;
    }
}
