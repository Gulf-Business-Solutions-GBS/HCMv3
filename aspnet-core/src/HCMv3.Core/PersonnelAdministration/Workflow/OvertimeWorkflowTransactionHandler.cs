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
/// Workflow handler for overtime (<see cref="EmployeeOvertime"/>).
///
/// <para>
/// Overtime is claimed for work already done, so unlike leave there is nothing to reserve -- the hours exist
/// whether or not anyone approves them. What approval decides is whether they get PAID, which is exactly what
/// <c>WorkflowStatus</c> plus the <c>ApprovedOnly</c> filter expresses: an unapproved claim is invisible to
/// payroll without payroll needing a single line about workflow.
/// </para>
///
/// <para>
/// The duplicate check is per employee per day per overtime type, because that is the granularity the entity
/// records. Claiming the same day twice is the mistake worth catching, and it must count pending claims or
/// the second submission would not see the first.
/// </para>
/// </summary>
public class OvertimeWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeOvertime>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for overtime.</summary>
    public const string OvertimeRequestType = "EmployeeOvertime";

    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OvertimeWorkflowTransactionHandler(
        IRepository<EmployeeOvertime> repository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _overtimeTypeRepository = overtimeTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => OvertimeRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var overtime = await GetTargetAsync(context);
        if (overtime == null)
        {
            return;
        }

        var hours = overtime.OvertimeHours > 0 ? overtime.OvertimeHours : overtime.ActualOvertimeHours;

        if (hours <= 0)
        {
            throw new UserFriendlyException(L("OvertimeHoursMustBeGreaterThanZero"));
        }

        if (hours > 24)
        {
            throw new UserFriendlyException(L("OvertimeHoursCannotExceedTwentyFourInADay"));
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var alreadyClaimed = Repository.GetAll()
                .Any(o => o.Id != overtime.Id
                          && o.EmployeeId == overtime.EmployeeId
                          && o.OvertimeTypeId == overtime.OvertimeTypeId
                          && o.EffectiveDate == overtime.EffectiveDate
                          && (o.WorkflowStatus == null
                              || o.WorkflowStatus == RequestStatus.Approved
                              || o.WorkflowStatus == RequestStatus.Closed
                              || o.WorkflowStatus == RequestStatus.Open));

            if (alreadyClaimed)
            {
                throw new UserFriendlyException(L("OvertimeForThisDateIsAlreadyClaimed"));
            }
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var overtime = await GetTargetAsync(context);
        if (overtime == null)
        {
            return null;
        }

        var overtimeType = _overtimeTypeRepository.GetAll().FirstOrDefault(t => t.Id == overtime.OvertimeTypeId);
        var hours = (overtime.OvertimeHours > 0 ? overtime.OvertimeHours : overtime.ActualOvertimeHours).ToString("0.##");
        var on = overtime.EffectiveDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{overtimeType?.NameEn ?? "Overtime"}, {hours} hour(s) on {on}",
            $"{overtimeType?.NameAr ?? "عمل إضافي"}، {hours} ساعة في {on}");
    }
}
