using System.Collections.Generic;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Answers one question: while somebody is away, WHO stands in for them?
///
/// <para>
/// An <c>EmployeeDelegation</c> row has existed since the port, and until now it did exactly one thing --
/// copy a covering colleague on the delegator's emails. It could not make that colleague able to
/// <em>act</em>, so an approval chain still stopped dead at whoever was on leave. This is the piece that
/// makes the row mean what its screen says it means.
/// </para>
///
/// <para>
/// Split out as its own service rather than inlined into <see cref="IWorkflowApproverResolver"/> because
/// three separate places need the identical answer -- step routing, escalation, and the notification CC --
/// and three copies of "which delegation is active today" is how they end up subtly disagreeing about
/// whether the last day counts.
/// </para>
/// </summary>
public interface IWorkflowDelegationResolver
{
    /// <summary>
    /// The employee currently standing in for <paramref name="employeeId"/>, or null when nobody is.
    ///
    /// <para>
    /// Exactly ONE hop. If the delegate has themselves delegated onwards, that is not followed: authority
    /// handed to a specific named person is not theirs to pass along, and a chain is also how a two-line
    /// data-entry mistake ends up letting somebody three desks away approve a salary change.
    /// </para>
    /// </summary>
    int? ResolveDelegateFor(int employeeId);

    /// <summary>
    /// Whether anybody has currently delegated TO <paramref name="employeeId"/>. A cheap pre-check so the
    /// ordinary approval -- the overwhelming majority -- does not pay for the substitution lookup.
    /// </summary>
    bool HasActiveDelegationTo(int employeeId);

    /// <summary>
    /// Replaces each delegator in the list with their stand-in, preserving order and dropping duplicates.
    /// Anyone without an active delegation is returned unchanged.
    /// </summary>
    List<int> ApplySubstitution(IEnumerable<int> candidateEmployeeIds);

    /// <summary>
    /// The substitutions <see cref="ApplySubstitution"/> would make over the same input, keyed by the
    /// STAND-IN and valued by the person they are standing in for. This is what lets the audit trail say
    /// "Sara, on behalf of Ali" instead of quietly recording Sara as the approver the chain nominated.
    /// </summary>
    IReadOnlyDictionary<int, int> BuildSubstitutionMap(IEnumerable<int> candidateEmployeeIds);
}
