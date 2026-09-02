using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for delegation of duties (<see cref="EmployeeDelegation"/>) -- "cover for me while I am
/// away".
///
/// <para>
/// <b>Why this type matters more than its size suggests.</b> An <see cref="EmployeeDelegation"/> row already
/// has a system consequence today: <c>EmailService</c> reads it to copy the covering colleague on the
/// delegator's notifications, and it is the row an approver-substitution branch reads. What it has never had
/// is a way to be created other than an administrator inserting it by hand. Routing it is what turns cover
/// into something an employee can ask for and a manager can grant.
/// </para>
///
/// <para>
/// <b>There is no separate request table</b> (legacy <c>DelegationRequest</c> existed only to copy itself
/// onto an <c>EmployeeDelegation</c> on approval). The delegation row IS the request, so
/// <see cref="OnApprovedAsync"/> has nothing to do beyond what the base does: stamping the row Approved is
/// precisely what makes the cover take effect, because that is the moment the <c>ApprovedOnly</c> filter
/// starts showing the row to the code that reads it.
/// </para>
/// </summary>
public class DelegationWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeDelegation>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for delegations.</summary>
    public const string DelegationRequestType = "EmployeeDelegation";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DelegationWorkflowTransactionHandler(
        IRepository<EmployeeDelegation> repository,
        IRepository<Employee> employeeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _employeeRepository = employeeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => DelegationRequestType;

    /// <summary>
    /// Legacy <c>DelegationRequest.PerformValidation</c> parity, plus one check legacy lacked.
    ///
    /// <para>
    /// Ported as-is: employee set, delegate set, dates set, end not before start, and -- the substantive one
    /// -- no overlap with another delegation for the same employee.
    /// </para>
    /// <para>
    /// Added here: <b>a delegation to yourself is refused.</b> Legacy allowed it. The row looks like cover
    /// and reads like cover to everything downstream, but hands the delegator's authority back to the
    /// delegator, so requests would sit waiting on somebody who is away -- an outage that looks like cover
    /// is worse than no cover, because nobody goes looking for it.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var delegation = await GetTargetAsync(context);
        if (delegation == null)
        {
            return;
        }

        if (delegation.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (delegation.DelegateToId <= 0)
        {
            throw new UserFriendlyException(L("DelegateToIsRequired"));
        }

        if (delegation.DelegateToId == delegation.EmployeeId)
        {
            throw new UserFriendlyException(L("CannotDelegateToYourself"));
        }

        if (delegation.StartDate == default || delegation.EndDate == default)
        {
            throw new UserFriendlyException(L("StartDateAndEndDateAreRequired"));
        }

        if (delegation.EndDate.Date < delegation.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Legacy DelegationRequest.PerformValidation lines 169-174: a second delegation covering any of the
        // same days is a conflict, because two people would each believe they held the delegator's
        // authority for the overlap.
        //
        // The opt-out is what makes it mean anything. An in-flight delegation is Draft/Open, which is
        // exactly what the ApprovedOnly filter hides -- so without this the same cover could be requested
        // twice, each request passing its own check, and both would land in an approver's inbox.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var overlapping = Repository.GetAll()
                .Any(d => d.Id != delegation.Id
                          && d.EmployeeId == delegation.EmployeeId
                          && d.StartDate <= delegation.EndDate
                          && d.EndDate >= delegation.StartDate
                          && (d.WorkflowStatus == null
                              || d.WorkflowStatus == RequestStatus.Approved
                              || d.WorkflowStatus == RequestStatus.Closed
                              || d.WorkflowStatus == RequestStatus.Open));

            if (overlapping)
            {
                throw new UserFriendlyException(L("OverlappingDelegationExists"));
            }
        }
    }

    /// <summary>
    /// Who is covering for whom, and for how long.
    ///
    /// <para>
    /// The delegate's NAME is the point of this summary, not a decoration. What is being approved is a
    /// transfer of the delegator's authority to a specific person: an approver shown only "Delegation,
    /// 3 Sep - 12 Sep" is being asked to agree to dates while the part that carries the consequence -- who
    /// gets to act in this employee's place -- stays off the screen.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var delegation = await GetTargetAsync(context);
        if (delegation == null)
        {
            return null;
        }

        var employee = _employeeRepository.GetAll()
            .FirstOrDefault(e => e.Id == delegation.EmployeeId);
        var delegateTo = _employeeRepository.GetAll()
            .FirstOrDefault(e => e.Id == delegation.DelegateToId);

        var range = delegation.StartDate.ToString("dd MMM yyyy")
                    + " - " + delegation.EndDate.ToString("dd MMM yyyy");

        var delegateEn = delegateTo?.FullNameEn ?? delegateTo?.Code ?? "delegate";
        var delegateAr = delegateTo?.FullNameAr ?? delegateTo?.Code ?? "المفوض";
        var employeeEn = employee?.FullNameEn ?? employee?.Code ?? "employee";
        var employeeAr = employee?.FullNameAr ?? employee?.Code ?? "الموظف";

        return new WorkflowSummary(
            $"Delegation to {delegateEn} for {employeeEn}, {range}",
            $"تفويض إلى {delegateAr} عن {employeeAr}، {range}");
    }

    /// <summary>
    /// Base only, deliberately.
    ///
    /// <para>
    /// There is nothing to create, copy or post. The base stamps the row Approved, and that single write is
    /// the delegation taking effect -- the engine and the email service already read this table, and the
    /// filter is what was keeping the row out of their sight. A handler that additionally "activated" the
    /// delegation somewhere would be inventing a second source of truth for the same fact.
    /// </para>
    /// </summary>
    public override Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        return base.OnApprovedAsync(context);
    }
}
