using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TimeManagement.Workflow;

/// <summary>
/// Workflow handler for a batch of calculated timesheet days (<see cref="TimeLogApproval"/>) -- the legacy
/// <c>TimeLogRequest</c>.
///
/// <para>
/// NOT self-service. A manager or HR reviews the engine's output for a department and proposes corrections;
/// an employee does not approve their own attendance. The catalog row therefore has no ESS route and no ESS
/// permission, exactly like <c>EmployeeAction</c>.
/// </para>
///
/// <para>
/// <b>The one thing this handler exists to get right.</b> Legacy wrote the corrected figures onto
/// <see cref="TimeRegister"/> at submit time and left approval with nothing to do but flip a status, so a
/// corrected timesheet was live before anybody approved it. Here <see cref="OnApprovedAsync"/> is the ONLY
/// place that touches <see cref="TimeRegister"/>: until it runs, the day still reads exactly what the engine
/// calculated, and a rejected batch changes nothing at all.
/// </para>
/// </summary>
public class TimeLogApprovalWorkflowTransactionHandler : WorkflowTransactionHandlerBase<TimeLogApproval>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for time log approvals.</summary>
    public const string TimeLogApprovalRequestType = "TimeLogApproval";

    private readonly IRepository<TimeLogApprovalDetail> _detailRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimePolicy> _timePolicyRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TimeLogApprovalWorkflowTransactionHandler(
        IRepository<TimeLogApproval> repository,
        IRepository<TimeLogApprovalDetail> detailRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimePolicy> timePolicyRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _detailRepository = detailRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _timePolicyRepository = timePolicyRepository;
        _employeeRepository = employeeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => TimeLogApprovalRequestType;

    /// <summary>
    /// Three refusals, all of which legacy lacked.
    ///
    /// <para>
    /// <b>An empty batch.</b> Legacy set a message string on the view model and carried on submitting, so an
    /// approver could be asked to approve nothing and the request would complete having changed no day at
    /// all. That is a request whose approval means nothing, and it is refused.
    /// </para>
    ///
    /// <para>
    /// <b>A day already in another in-flight batch.</b> Two requests proposing different figures for the same
    /// day would both be approvable, and whichever was approved second would silently overwrite the first.
    /// </para>
    ///
    /// <para>
    /// <b>A day already approved and not since reopened.</b> Re-approving a settled day is how an approved
    /// figure gets quietly replaced without the history row the engine's reopen path would otherwise have
    /// written. Correcting such a day is legitimate, but it has to go through recalculation, which reopens it.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var approval = await GetTargetAsync(context);
        if (approval == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var timeRegisterIds = _detailRepository.GetAll()
                .Where(d => d.TimeLogApprovalId == approval.Id)
                .Select(d => d.TimeRegisterId)
                .ToList();

            if (timeRegisterIds.Count == 0)
            {
                throw new UserFriendlyException(L("TimeLogApprovalHasNoDays"));
            }

            // In flight = raised and not yet decided. A rejected or sent-back batch releases its days.
            var contested = _detailRepository.GetAll()
                .Any(d => d.TimeLogApprovalId != approval.Id
                          && timeRegisterIds.Contains(d.TimeRegisterId)
                          && (d.TimeLogApproval.WorkflowStatus == RequestStatus.Draft
                              || d.TimeLogApproval.WorkflowStatus == RequestStatus.Open));

            if (contested)
            {
                throw new UserFriendlyException(L("TimeRegisterAlreadyInAnotherTimeLogApproval"));
            }

            var settled = _timeRegisterRepository.GetAll()
                .Any(r => timeRegisterIds.Contains(r.Id) && r.Status == TimeRegisterStatus.Approved);

            if (settled)
            {
                throw new UserFriendlyException(L("TimeRegisterAlreadyApproved"));
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// THE write. Copies each detail's proposed figures onto its <see cref="TimeRegister"/>, recomputes
    /// <see cref="TimeRegister.ApprovedActualWorkingDay"/> from the day's own minutes, marks the day
    /// <see cref="TimeRegisterStatus.Approved"/> and stamps <see cref="TimeRegister.TimeLogApprovalId"/> so
    /// the engine can find and invalidate the approval later.
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        var approval = await GetRequiredTargetAsync(context);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var details = _detailRepository.GetAll()
                .Where(d => d.TimeLogApprovalId == approval.Id)
                .ToList();

            if (details.Count == 0)
            {
                return;
            }

            var timeRegisterIds = details.Select(d => d.TimeRegisterId).ToList();

            var timeRegisters = _timeRegisterRepository.GetAll()
                .Where(r => timeRegisterIds.Contains(r.Id))
                .ToList()
                .ToDictionary(r => r.Id);

            var policyIds = timeRegisters.Values
                .Where(r => r.TimePolicyId.HasValue)
                .Select(r => r.TimePolicyId.Value)
                .Distinct()
                .ToList();

            var toleranceByPolicyId = _timePolicyRepository.GetAll()
                .Where(p => policyIds.Contains(p.Id))
                .Select(p => new { p.Id, p.MonthlyToleranceType })
                .ToList()
                .ToDictionary(p => p.Id, p => p.MonthlyToleranceType);

            foreach (var detail in details)
            {
                if (!timeRegisters.TryGetValue(detail.TimeRegisterId, out var timeRegister))
                {
                    // The day was deleted between submission and approval. Nothing to write; skipping is
                    // right, because the alternative is failing an approval over a row that no longer exists.
                    continue;
                }

                timeRegister.ApprovedAbsentDay = detail.ProposedAbsentDay;
                timeRegister.ApprovedDedLateInEarlyOutMins = detail.ProposedDedLateInEarlyOutMins;

                var tolerance = timeRegister.TimePolicyId.HasValue
                                && toleranceByPolicyId.TryGetValue(timeRegister.TimePolicyId.Value, out var found)
                    ? found
                    : MonthlyToleranceType.NotSelected;

                timeRegister.ApprovedActualWorkingDay = CalculateApprovedActualWorkingDay(timeRegister, tolerance);

                timeRegister.Status = TimeRegisterStatus.Approved;
                timeRegister.TimeLogApprovalId = approval.Id;

                await _timeRegisterRepository.UpdateAsync(timeRegister);
            }
        }
    }

    /// <summary>
    /// Ported verbatim from legacy <c>HCM10.ESS/Repository/TimeLogRepository.cs</c> lines 311-336 -- the same
    /// arithmetic, moved from submit time to approval time, which is the whole point of this port.
    ///
    /// <para>
    /// Reading it in order: a day whose absence verdict was overturned is credited its FULL planned minutes,
    /// not its actual ones, because forgiving the absence means treating the employee as present. A day whose
    /// deductible late minutes were reduced gets the difference handed back -- but ONLY when the policy's
    /// <see cref="MonthlyToleranceType"/> is <c>NotSelected</c>, because the other two settings apply
    /// tolerance across the whole MONTH and adding it back here as well would forgive the same minutes twice.
    /// The <c>ActualAbsenceMins != 0</c> guard keeps that credit off a day that had no absence to deduct for
    /// in the first place.
    /// </para>
    ///
    /// <para>
    /// Then the minutes become a day: a day carrying leave is worth <c>1 - LeaveDay</c> outright, otherwise
    /// it is the minutes ratio, and the result is clamped into [0, 1] because neither a negative working day
    /// nor more than one working day in one day means anything to payroll.
    /// </para>
    ///
    /// <para>
    /// One deliberate deviation: legacy dereferenced <c>timeRegister.TimePolicy.MonthlyToleranceType</c>
    /// unguarded and threw when a day had no policy. Here a day without a policy has no monthly tolerance
    /// configured, which is the same thing <c>NotSelected</c> means, so it takes that branch instead of
    /// failing the approval. Resolved by the caller.
    /// </para>
    /// </summary>
    public static double CalculateApprovedActualWorkingDay(TimeRegister timeRegister, MonthlyToleranceType monthlyToleranceType)
    {
        var approvedActualWorkingMins = timeRegister.ActualWorkingMins;

        if (timeRegister.AbsentDay != timeRegister.ApprovedAbsentDay)
        {
            approvedActualWorkingMins = timeRegister.PlannedWorkingMins;
        }

        if (timeRegister.DedLateInEarlyOutMins != timeRegister.ApprovedDedLateInEarlyOutMins
            && timeRegister.ActualAbsenceMins != 0
            && monthlyToleranceType == MonthlyToleranceType.NotSelected)
        {
            approvedActualWorkingMins = approvedActualWorkingMins
                                        + timeRegister.DedLateInEarlyOutMins
                                        - timeRegister.ApprovedDedLateInEarlyOutMins;
        }

        double approvedActualWorkingDay;

        if (timeRegister.PlannedWorkingMins != 0 && timeRegister.LeaveDay != 0)
        {
            approvedActualWorkingDay = Math.Round(1 - timeRegister.LeaveDay, 3);
        }
        else if (timeRegister.PlannedWorkingMins != 0 && approvedActualWorkingMins != 0)
        {
            approvedActualWorkingDay = Math.Round(approvedActualWorkingMins / timeRegister.PlannedWorkingMins, 3);
        }
        else
        {
            approvedActualWorkingDay = 0;
        }

        if (approvedActualWorkingDay > 1)
        {
            approvedActualWorkingDay = 1;
        }

        return Math.Round(approvedActualWorkingDay < 0 ? 0 : approvedActualWorkingDay, 3);
    }

    /// <summary>
    /// Leads with WHOSE days and HOW MANY, because that is the size of the decision -- "7 days" and "1 day"
    /// are not the same request even when everything else about them matches. The date range bounds it and
    /// the org unit is shown when the batch came from a department grid.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var approval = await GetTargetAsync(context);
        if (approval == null)
        {
            return null;
        }

        int dayCount;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            dayCount = _detailRepository.GetAll().Count(d => d.TimeLogApprovalId == approval.Id);
        }

        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == approval.EmployeeId)
            .Select(e => new { e.FullNameEn, e.FullNameAr })
            .FirstOrDefault();

        var range = approval.StartDate.ToString("dd MMM yyyy") + " - " + approval.EndDate.ToString("dd MMM yyyy");

        var subjectEn = employee?.FullNameEn ?? "Employee";
        var subjectAr = employee?.FullNameAr ?? "موظف";

        return new WorkflowSummary(
            $"Time log approval, {subjectEn}, {range}, {dayCount} day(s)",
            $"اعتماد سجل الوقت، {subjectAr}، {range}، {dayCount} يوم");
    }

    /// <summary>
    /// The days this batch covers, in one query. Exposed for callers that need the set without going through
    /// the navigation property (which EF is not guaranteed to have loaded).
    /// </summary>
    protected virtual List<int> GetTimeRegisterIds(int approvalId)
    {
        return _detailRepository.GetAll()
            .Where(d => d.TimeLogApprovalId == approvalId)
            .Select(d => d.TimeRegisterId)
            .ToList();
    }
}
