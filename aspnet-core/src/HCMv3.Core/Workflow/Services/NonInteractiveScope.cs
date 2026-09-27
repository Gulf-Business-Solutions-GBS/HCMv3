using System;
using System.Threading;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Marks a stretch of work as non-interactive -- an import, a data load, a scheduled job -- so that
/// anything inside it is refused the approval machinery.
///
/// <para>
/// The rule this enforces: <b>routing is something a person asks for.</b> A go-live history load of five
/// years of leave for five hundred employees is twenty thousand rows, and if those were routed it would be
/// twenty thousand approval requests demanding decisions about leave taken years ago. Nobody would ever
/// want that, which is why no importer calls the submitter today -- but that is a fact about how the
/// import code happens to be written, not a rule, and the next import screen somebody writes will look
/// exactly like an admin screen to whoever writes it.
/// </para>
///
/// <para>
/// So the guard is here rather than in a review checklist. A path that opens this scope and then reaches
/// the submitter fails loudly and immediately, at the first row, in whatever test or dev run first touches
/// it -- instead of succeeding and being discovered by a manager with twenty thousand unread approvals.
/// </para>
///
/// <para>
/// Deliberately NOT a stored flag on the row. A flag records what happened after the fact and can be
/// forgotten at the one call site that matters; this makes the wrong thing impossible while it is being
/// written. Marking imported rows is a separate, useful thing and is not what this does.
/// </para>
/// </summary>
public static class NonInteractiveScope
{
    // AsyncLocal, so the mark follows the work across awaits and does not leak into unrelated requests
    // sharing the thread -- which a [ThreadStatic] would do, and which under a web server would mean an
    // import silently disabling routing for whoever else's request landed on that thread next.
    private static readonly AsyncLocal<string> _reason = new();

    /// <summary>True while the current work is running inside a non-interactive scope.</summary>
    public static bool IsActive => _reason.Value != null;

    /// <summary>Why the current scope was opened, for the message when something is refused.</summary>
    public static string Reason => _reason.Value;

    /// <summary>

    /// <summary>
    /// Refuses the approval machinery to non-interactive work. Called at both routing chokepoints -- the
    /// admin submitter, which creates the request, and the approval service, which sends it -- so a path
    /// that reaches either one fails before it writes anything.
    ///
    /// <para>
    /// Deliberately <see cref="InvalidOperationException"/> and not a user-friendly error: there is no
    /// user on these paths, and this must read as the programming error it is rather than something an
    /// operator could act on.
    /// </para>
    /// </summary>
    public static void ThrowIfActive()
    {
        if (!IsActive)
        {
            return;
        }

        throw new InvalidOperationException(
            "Workflow routing was requested from " + Reason + ", which is a non-interactive path. " +
            "Imports, data loads and background jobs must write through the repository directly and " +
            "never raise approval requests -- a go-live history load routed would be tens of thousands " +
            "of approvals for transactions from years ago. If this genuinely needs routing, it has to be " +
            "raised by a person on a screen.");
    }
    /// <summary>
    /// Opens a scope. Nesting is allowed and the outermost reason wins -- an importer calling another
    /// importer should not quietly unmark itself when the inner one finishes.
    /// </summary>
    /// <param name="reason">Short description of the work, e.g. "bulk import" -- shown if something is refused.</param>
    public static IDisposable Begin(string reason)
    {
        var previous = _reason.Value;
        _reason.Value = previous ?? (reason ?? "non-interactive work");

        return new Handle(previous);
    }

    private sealed class Handle : IDisposable
    {
        private readonly string _previous;
        private bool _disposed;

        public Handle(string previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _reason.Value = _previous;
        }
    }
}
