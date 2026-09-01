using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.ManpowerPlanningManagement.Workflow;

/// <summary>
/// Workflow handler for manpower plans (<see cref="ManpowerPlanning"/>) -- the budgeted headcount a
/// department is asking for over a planning period.
///
/// <para>
/// THIS TYPE REPLACED AN APPROVAL PATH RATHER THAN ADDING ONE. <c>ManpowerPlanningsAppService</c> used to
/// carry its own <c>Approve</c>/<c>Reject</c> methods writing a stored <c>ManpowerPlanningStatus</c> column.
/// Both are deleted. Everything approval-shaped about a manpower plan now happens here: the engine writes
/// <c>WorkflowStatus</c>, the entity projects <c>ManpowerPlanningStatus</c> off it, and the legacy
/// "Entry On Final Approval" side effect -- creating a real <see cref="Position"/> for every planned line --
/// hangs off <see cref="OnApprovedAsync"/> so it fires once, from one place, when the chain actually
/// completes.
/// </para>
///
/// <para>
/// NOT self-service, like personnel actions: a manager plans headcount for a department, an employee does
/// not request it about themselves. Its catalog row therefore has no ESS route and no ESS permission.
/// </para>
///
/// <para>
/// Routing this type is worth it precisely because of that side effect. An approved plan mints budgeted,
/// vacant positions that recruitment then hires against, so a plan approved by nobody would put headcount
/// and salary bands into the establishment on one person's say-so.
/// </para>
/// </summary>
public class ManpowerPlanningWorkflowTransactionHandler : WorkflowTransactionHandlerBase<ManpowerPlanning>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for manpower plans.</summary>
    public const string ManpowerPlanningRequestType = "ManpowerPlanning";

    private readonly IRepository<ManpowerPlanningDetail> _detailRepository;
    private readonly IRepository<ManpowerPlanningPeriod> _periodRepository;
    private readonly IRepository<Position> _positionRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;

    public ManpowerPlanningWorkflowTransactionHandler(
        IRepository<ManpowerPlanning> repository,
        IRepository<ManpowerPlanningDetail> detailRepository,
        IRepository<ManpowerPlanningPeriod> periodRepository,
        IRepository<Position> positionRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _detailRepository = detailRepository;
        _periodRepository = periodRepository;
        _positionRepository = positionRepository;
        _organizationUnitRepository = organizationUnitRepository;
    }

    public override string RequestType => ManpowerPlanningRequestType;

    /// <summary>
    /// Refuses an empty plan and a plan with no period.
    ///
    /// <para>
    /// An empty plan is the failure worth guarding: approval is what creates the positions, so a plan with
    /// no lines asks an approver to agree to nothing and then quietly creates nothing. The period matters
    /// for the same reason -- it supplies the start date every created position is dated from.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return;
        }

        if (plan.PeriodId <= 0 || !_periodRepository.GetAll().Any(p => p.Id == plan.PeriodId))
        {
            throw new UserFriendlyException(L("PeriodCannotBeBlank"));
        }

        var hasDetails = _detailRepository.GetAll().Any(d => d.ManpowerPlanningId == plan.Id);
        if (!hasDetails)
        {
            throw new UserFriendlyException(L("ManpowerPlanningMustHaveAtLeastOneLine"));
        }
    }

    /// <summary>
    /// The one-line inbox label: which department, which period, how much. That triple is the decision --
    /// an approver is agreeing to spend a stated amount on headcount for a stated department and year, and
    /// nothing else on the row tells them that.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return null;
        }

        var organizationUnit = _organizationUnitRepository.GetAll()
            .Where(o => o.Id == plan.OrganizationUnitId)
            .Select(o => o.DisplayName)
            .FirstOrDefault();

        var period = _periodRepository.GetAll()
            .Where(p => p.Id == plan.PeriodId)
            .Select(p => p.Name)
            .FirstOrDefault();

        var unitLabel = string.IsNullOrWhiteSpace(organizationUnit) ? "-" : organizationUnit;
        var periodLabel = string.IsNullOrWhiteSpace(period) ? "-" : period;
        var amount = plan.GrandTotalAmount.ToString("N2", CultureInfo.InvariantCulture);

        return new WorkflowSummary(
            $"Manpower Plan, {unitLabel}, {periodLabel}, total {amount}",
            $"خطة القوى العاملة، {unitLabel}، {periodLabel}، الإجمالي {amount}");
    }

    /// <summary>
    /// Legacy "Entry On Final Approval" parity, moved here from the deleted
    /// <c>ManpowerPlanningsAppService.Approve</c>: for every <see cref="ManpowerPlanningDetail"/> that has
    /// no <see cref="Position"/> yet, create one from the detail's job title and the period's start date,
    /// with the same field assignments the legacy <c>OnSaving</c> block used (<c>NameEn</c>/<c>NameAr</c>
    /// from the job title, <c>EndDate</c> = 9999-12-31, <c>Vacant</c>/<c>Active</c>/<c>IsBudgeted</c> = true,
    /// <c>FTELimit</c> = 1, Min/Mid/Max salary all set to the line's January amount).
    ///
    /// <para>
    /// It hangs off approval rather than off a button because that is the only event that means the plan was
    /// agreed. There is now exactly one code path that mints these positions.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var plan = await GetTargetAsync(context);
        if (plan == null)
        {
            return;
        }

        await CreatePlannedPositionsAsync(plan);
    }

    private async Task CreatePlannedPositionsAsync(ManpowerPlanning plan)
    {
        var period = _periodRepository.GetAll().FirstOrDefault(p => p.Id == plan.PeriodId);
        if (period == null)
        {
            // Validation refuses a plan without a period at submit; a plan that lost it afterwards is a data
            // problem, not something to guess a start date for.
            throw new UserFriendlyException(L("PeriodCannotBeBlank"));
        }

        var detailsWithoutPosition = _detailRepository.GetAll()
            .Where(d => d.ManpowerPlanningId == plan.Id && d.PositionId == null)
            .Select(d => new
            {
                d.Id,
                d.JobTitleId,
                d.January,
                JobTitleNameEn = d.JobTitle.NameEn,
                JobTitleNameAr = d.JobTitle.NameAr
            })
            .ToList();

        if (!detailsWithoutPosition.Any())
        {
            return;
        }

        var nextCode = await GetNextPositionCodeAsync();

        foreach (var detail in detailsWithoutPosition)
        {
            var position = new Position
            {
                TenantId = plan.TenantId,
                Code = nextCode.ToString(CultureInfo.InvariantCulture),
                NameEn = detail.JobTitleNameEn,
                NameAr = detail.JobTitleNameAr,
                StartDate = period.StartDate,
                EndDate = new DateTime(9999, 12, 31),
                Vacant = true,
                Active = true,
                JobTitleId = detail.JobTitleId,
                MinSalary = detail.January,
                MidSalary = detail.January,
                MaxSalary = detail.January,
                IsBudgeted = true,
                FTELimit = 1
            };

            var positionId = await _positionRepository.InsertAndGetIdAsync(position);
            nextCode++;

            var detailRow = await _detailRepository.GetAsync(detail.Id);
            detailRow.PositionId = positionId;
            await _detailRepository.UpdateAsync(detailRow);
        }
    }

    /// <summary>
    /// Legacy <c>Position.PerformValidation</c> auto-Code parity (max existing numeric code + 1), needed
    /// here because approval creates positions directly rather than through <c>PositionsAppService</c>.
    /// </summary>
    private async Task<int> GetNextPositionCodeAsync()
    {
        var codes = await Task.FromResult(_positionRepository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToList());

        var maxCode = codes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxCode + 1;
    }
}
