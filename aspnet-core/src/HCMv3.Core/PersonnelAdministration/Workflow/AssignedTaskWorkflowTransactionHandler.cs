using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for assigned tasks (<see cref="EmployeeAssignedTask"/>, legacy <c>TaskRequest</c>,
/// legacy request code "17").
///
/// <para>
/// Like <see cref="ITServiceWorkflowTransactionHandler"/> and
/// <see cref="EmployeeActionWorkflowTransactionHandler"/> this type is routed but NOT self-service: a task is
/// ASSIGNED, so the person raising it and the person it lands on are deliberately different people. Legacy
/// says so in its own code -- <c>SysCreator</c> and <c>Employee</c> are separate fields, and the
/// <c>IsCreator</c> appearance rules exist precisely so the assignee cannot rewrite the job they have been
/// given. So submission happens in <c>EmployeeAssignedTasksAppService</c> through
/// <c>IAdminTransactionSubmitter</c>, where the employee comes from the ENTITY rather than the session, and
/// the catalog row carries no ESS route and no ESS permission.
/// </para>
///
/// <para>
/// <b>There is no approval side effect, and that is not an omission.</b> Nothing in legacy runs when a
/// <c>TaskRequest</c> reaches Approved -- not in <c>OnSaving</c>, not in <c>WorkFlowServices</c>, not in
/// <c>RequestProcessApproval</c>; the only type-specific code on the approval path appends the approver's
/// comment on REJECTION, which this port keeps properly as <c>WorkflowComment</c> rows. Under the
/// single-table design the row simply becomes visible and effective when the chain completes. The base class
/// stamping <c>WorkflowStatus</c> is therefore the whole of what approval does here, and a future maintainer
/// should not read the empty <c>OnApprovedAsync</c> as something waiting to be filled in.
/// </para>
///
/// <para>
/// The base class also decides what happens for an amendment (<c>context.IsAmendment</c>): it leaves a live
/// row alone at submit, reject and send-back, and only applies proposed values on approval. This handler
/// leaves <see cref="WorkflowTransactionHandlerBase{TEntity}.SupportsAmendment"/> false, so no amendment can
/// be raised against a task in the first place and the guard other handlers need in their own hooks -- the
/// <c>if (context.IsAmendment) return;</c> that stops a side effect running twice -- has nothing to guard
/// here. If amendments are ever switched on for this type, the validation below reads the ROW, which for an
/// amendment still holds the OLD values; it would have to read the snapshot instead, as the leave handler
/// does.
/// </para>
///
/// <para>
/// The validation is legacy's <c>PerformValidations</c>, rule for rule, with two changes. It throws
/// <see cref="UserFriendlyException"/> with localized keys rather than raw <c>Exception</c> with hardcoded
/// English, and it is not wrapped in legacy's <c>catch (Exception ex) { throw ex; }</c>, which discarded the
/// stack trace of anything that went wrong. Note also that legacy's <c>OnSaving</c> never called
/// <c>base.OnSaving()</c>, so what that class actually did on save was never what it was written to do --
/// which is why its runtime behaviour is not treated as the specification here; the four rules it states are.
/// </para>
/// </summary>
public class AssignedTaskWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAssignedTask>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for assigned tasks.</summary>
    public const string AssignedTaskRequestType = "EmployeeAssignedTask";

    public AssignedTaskWorkflowTransactionHandler(
        IRepository<EmployeeAssignedTask> repository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
    }

    public override string RequestType => AssignedTaskRequestType;

    /// <summary>
    /// Legacy's four checks: an assignee, a subject, both dates, and an end date that is not before the
    /// start. Legacy phrased the last one as a plain <c>&lt;</c> comparison on full <c>DateTime</c> values;
    /// compared on <c>Date</c> here so a task that starts and ends on the same day cannot be refused because
    /// of a time component nobody entered.
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var task = await GetTargetAsync(context);
        if (task == null)
        {
            return;
        }

        if (task.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (string.IsNullOrWhiteSpace(task.Subject))
        {
            throw new UserFriendlyException(L("TaskSubjectIsRequired"));
        }

        if (task.StartDate == default)
        {
            throw new UserFriendlyException(L("StartDateIsRequired"));
        }

        if (task.EndDate == default)
        {
            throw new UserFriendlyException(L("EndDateIsRequired"));
        }

        if (task.EndDate.Date < task.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    /// <summary>
    /// The one-line inbox label, e.g. "Prepare Q3 headcount pack, 01 Sep 2026 - 15 Sep 2026".
    ///
    /// <para>
    /// The SUBJECT leads, because it is the only thing that distinguishes one task from another -- unlike a
    /// leave or a loan there is no type, amount or category to summarise. An approver looking at a list of
    /// "Task Assignment" rows would have to open every one to find out what they were agreeing to. The
    /// subject is bounded at 200 characters on the entity and the summary column holds 512, so it cannot
    /// overflow the label.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var task = await GetTargetAsync(context);
        if (task == null)
        {
            return null;
        }

        var subjectEn = string.IsNullOrWhiteSpace(task.Subject) ? "Task" : task.Subject;
        var subjectAr = string.IsNullOrWhiteSpace(task.Subject) ? "مهمة" : task.Subject;

        var range = task.StartDate.ToString("dd MMM yyyy") + " - " + task.EndDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{subjectEn}, {range}",
            $"{subjectAr}، {range}");
    }
}
