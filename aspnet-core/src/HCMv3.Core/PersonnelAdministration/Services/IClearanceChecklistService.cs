using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Answers "what is THIS approver being asked to clear, and what have they said so far".
///
/// <para>
/// This is the whole of the multi-department behaviour, and it is deliberately a rendering rule rather than
/// a routing one. Routing is the generic engine's: a clearance sits on an <c>ApprovalRequire.Parallel</c>
/// step, so IT, Finance and HR are all approvers of the same step and the step stays open until every one of
/// them has acted. Nothing here duplicates that. What this service adds is the narrowing --
/// <c>EmployeeWorkflowRole</c> says which roles the signed-in approver currently holds,
/// <c>ClearanceRequestFieldDetail.WorkflowRoleId</c> says which role owns each checklist line, and the
/// intersection is their part of the form. Finance never sees the IT questions, and cannot answer them
/// either: the save path applies the same narrowing rather than trusting what the screen sent.
/// </para>
/// </summary>
public interface IClearanceChecklistService
{
    /// <summary>
    /// The checklist lines <paramref name="approverEmployeeId"/>'s roles own on this clearance, merged with
    /// any answers already saved -- so an approver returning to a half-finished form sees their own previous
    /// answers rather than a blank one. Empty when the approver holds no role that owns a line, which is the
    /// correct answer for somebody who is on the step for another reason entirely.
    /// </summary>
    Task<List<ClearanceChecklistLine>> GetLinesForApproverAsync(int employeeClearanceId, int approverEmployeeId);

    /// <summary>
    /// Records this approver's answers.
    ///
    /// <para>
    /// Refused unless the clearance is sitting on a step whose <c>ApprovalRequire</c> is <c>Parallel</c>.
    /// That is the only configuration in which several departments hold the record at once, and it is the
    /// only one where answering lines is meaningful: on a single-approver step the checklist has one owner
    /// and the questions belong to the form, not to the step.
    /// </para>
    /// <para>
    /// Lines the approver's roles do not own are refused rather than silently dropped -- a screen asking to
    /// write somebody else's answer is a bug or an attack, and either way the caller should hear about it.
    /// </para>
    /// </summary>
    /// <summary>
    /// Whether answers may be recorded right now, i.e. the clearance is open at a <c>Parallel</c> step. Lets
    /// a screen render read-only instead of collecting answers the save call would then refuse.
    /// </summary>
    Task<bool> IsAwaitingParallelApprovalAsync(int employeeClearanceId);

    Task SaveAnswersForApproverAsync(
        int employeeClearanceId,
        int approverEmployeeId,
        IReadOnlyCollection<ClearanceChecklistAnswer> answers);
}
