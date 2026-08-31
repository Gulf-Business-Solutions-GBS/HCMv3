using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for personnel actions (<see cref="EmployeeAction"/>) -- promotions, transfers,
/// re-gradings, separations.
///
/// <para>
/// THE FIRST TYPE THAT IS NOT SELF-SERVICE. HR raises a personnel action, not the employee, so its catalog
/// row is seeded workflow-capable but ESS-disabled: it appears in an approver's inbox and never on an
/// employee's tile list. The two flags on <c>WorkflowTransactionType</c> exist precisely so a type can be
/// routed for approval without also being something anybody can raise about themselves -- and a promotion
/// you can request for yourself is not an approval chain, it is a suggestion box.
/// </para>
///
/// <para>
/// The stakes are high enough to justify routing. An <c>EmployeeAction</c> is the parent of
/// <c>EmployeeOrgAssignment</c> and <c>EmployeeContract</c> rows, so an unapproved one moves somebody's job
/// title, cost centre and pay in one step. Keeping it invisible until approved means those dependents cannot
/// be built on a decision nobody made.
/// </para>
/// </summary>
public class EmployeeActionWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAction>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for personnel actions.</summary>
    public const string EmployeeActionRequestType = "EmployeeAction";

    private readonly IRepository<PersonalAction> _personalActionRepository;
    private readonly IRepository<PersonalActionReason> _reasonRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeActionWorkflowTransactionHandler(
        IRepository<EmployeeAction> repository,
        IRepository<PersonalAction> personalActionRepository,
        IRepository<PersonalActionReason> reasonRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _personalActionRepository = personalActionRepository;
        _reasonRepository = reasonRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => EmployeeActionRequestType;

    /// <summary>
    /// Refuses a second action of the same kind on the same day for the same employee -- the rule the admin
    /// service already enforces, restated here so it holds for a routed action too.
    ///
    /// <para>
    /// The in-flight rows have to be visible for this to mean anything. Without the opt-out, the admin
    /// service's own duplicate check cannot see a pending action either, so the same promotion could be
    /// raised twice and both would pass.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var action = await GetTargetAsync(context);
        if (action == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = Repository.GetAll()
                .Any(a => a.Id != action.Id
                          && a.EmployeeId == action.EmployeeId
                          && a.PersonalActionId == action.PersonalActionId
                          && a.StartDate == action.StartDate
                          && (a.WorkflowStatus == null
                              || a.WorkflowStatus == RequestStatus.Approved
                              || a.WorkflowStatus == RequestStatus.Closed
                              || a.WorkflowStatus == RequestStatus.Open));

            if (duplicate)
            {
                throw new UserFriendlyException(L("DuplicateEmployeeActionExists"));
            }
        }
    }

    /// <summary>
    /// Leads with the action and its reason, because that pair IS the decision -- "Promotion, Annual review"
    /// tells an approver what they are agreeing to in a way an employee name and a date do not. The inbox
    /// shows who it is about separately.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var action = await GetTargetAsync(context);
        if (action == null)
        {
            return null;
        }

        var personalAction = _personalActionRepository.GetAll()
            .Where(p => p.Id == action.PersonalActionId)
            .Select(p => new { p.NameEn, p.NameAr })
            .FirstOrDefault();

        var reason = _reasonRepository.GetAll()
            .Where(r => r.Id == action.ActionReasonId)
            .Select(r => new { r.NameEn, r.NameAr })
            .FirstOrDefault();

        var effective = action.StartDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{personalAction?.NameEn ?? "Action"}, {reason?.NameEn ?? "-"}, from {effective}",
            $"{personalAction?.NameAr ?? "إجراء"}، {reason?.NameAr ?? "-"}، من {effective}");
    }
}
