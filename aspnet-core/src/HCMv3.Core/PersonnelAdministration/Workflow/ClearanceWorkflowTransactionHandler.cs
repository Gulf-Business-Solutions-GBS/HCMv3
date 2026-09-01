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
/// Workflow handler for exit clearances (<see cref="EmployeeClearance"/>).
///
/// <para>
/// <b>There is no multi-department machinery here, and there was none in legacy either.</b> "IT, Finance and
/// HR each sign off" is an ordinary <c>ApprovalRequire.Parallel</c> step: the engine assigns every resolved
/// role holder as an approver of the same step and <c>WorkflowApprovalService.IsStepApproved</c> holds the
/// step open until all of them have acted. What each approver is asked about comes from
/// <see cref="ClearanceRequestFieldDetail.WorkflowRoleId"/> at render time, which is a display concern, not
/// a routing one. A handler is the wrong place to reinvent either half.
/// </para>
///
/// <para>
/// <b>No <c>OnApprovedAsync</c> override, on purpose.</b> Legacy's approval block on the clearance object is
/// commented out in full; approving a clearance writes nothing anywhere else. The base's status stamp -- the
/// thing that takes the row out of hiding -- is the whole of what approval means for this type. Terminating
/// the employee is a separate personnel action with its own routing.
/// </para>
///
/// <para>
/// Lives in PersonnelAdministration rather than Workflow, the same rule the other handlers follow: clearance
/// rules belong to the module that owns the employee lifecycle, and the engine stays type-agnostic.
/// </para>
/// </summary>
public class ClearanceWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeClearance>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for clearances.</summary>
    public const string ClearanceRequestType = "EmployeeClearance";

    private readonly IRepository<GeneralMasterInfo.ClearanceRequestType> _clearanceTypeRepository;

    public ClearanceWorkflowTransactionHandler(
        IRepository<EmployeeClearance> repository,
        IRepository<GeneralMasterInfo.ClearanceRequestType> clearanceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _clearanceTypeRepository = clearanceTypeRepository;
    }

    public override string RequestType => ClearanceRequestType;

    /// <summary>
    /// The complete legacy rule set: an employee, both dates, and an end date that is not before the start.
    /// Nothing about the checklist being finished -- legacy never refused a clearance for unanswered lines,
    /// and inventing that rule here would block the very requests departments are meant to answer as they go.
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var clearance = await GetTargetAsync(context);
        if (clearance == null)
        {
            return;
        }

        if (clearance.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (clearance.StartDate == default)
        {
            throw new UserFriendlyException(L("StartDateIsRequired"));
        }

        if (clearance.EndDate == default)
        {
            throw new UserFriendlyException(L("EndDateIsRequired"));
        }

        if (clearance.EndDate < clearance.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeBeforeStartDate"));
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var clearance = await GetTargetAsync(context);
        if (clearance == null)
        {
            return null;
        }

        var type = _clearanceTypeRepository.GetAll()
            .FirstOrDefault(t => t.Id == clearance.ClearanceRequestTypeId);

        var period = $"{clearance.StartDate:dd MMM yyyy} - {clearance.EndDate:dd MMM yyyy}";

        return new WorkflowSummary(
            $"{type?.NameEn ?? "Clearance"} clearance, {period}",
            $"إخلاء طرف {type?.NameAr ?? string.Empty}، {period}");
    }
}
