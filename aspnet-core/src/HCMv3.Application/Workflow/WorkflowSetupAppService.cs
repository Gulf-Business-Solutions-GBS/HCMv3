using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <inheritdoc cref="IWorkflowSetupAppService"/>
[AbpAuthorize(AppPermissions.Pages_Workflow_Setup)]
public class WorkflowSetupAppService : HCMv3AppServiceBase, IWorkflowSetupAppService
{
    /// <summary>
    /// The grid owns the chains it writes, and it recognises them by this prefix plus the transaction code.
    /// A transaction pointing at a chain the grid did not make is still readable, but it is only editable
    /// here if nothing else uses it -- see <c>DescribeLock</c>.
    /// </summary>
    private const string GridChainPrefix = "WF-";

    /// <summary>
    /// The one chain most transactions follow. Shared on purpose -- editing it is meant to change every
    /// transaction pointed at it, which is what makes it a default rather than a template.
    /// </summary>
    private const string DefaultChainCode = GridChainPrefix + "DEFAULT";

    /// <summary>How many approver positions the grid shows. A longer chain is locked, never truncated.</summary>
    private const int MaxSteps = 3;

    /// <summary>
    /// The notification setting the grid's email switch points at, created once per tenant on first use.
    /// One shared row rather than one per step: the switch is a yes/no, and giving each step its own
    /// identical config would leave a pile of rows nobody can tell apart in the detailed screens.
    /// </summary>
    private const string GridNotificationConfigName = "Workflow Setup - notify approver";

    private readonly IRepository<WorkflowTransactionType> _transactionTypeRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RequestDetailParallel> _requestDetailParallelRepository;
    private readonly IRepository<WorkflowRole> _workflowRoleRepository;
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<WorkflowGroupDetail> _workflowGroupDetailRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowSetupAppService(
        IRepository<WorkflowTransactionType> transactionTypeRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RequestDetailParallel> requestDetailParallelRepository,
        IRepository<WorkflowRole> workflowRoleRepository,
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<WorkflowGroupDetail> workflowGroupDetailRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _transactionTypeRepository = transactionTypeRepository;
        _requestMasterRepository = requestMasterRepository;
        _requestDetailRepository = requestDetailRepository;
        _requestDetailParallelRepository = requestDetailParallelRepository;
        _workflowRoleRepository = workflowRoleRepository;
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _workflowGroupDetailRepository = workflowGroupDetailRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeRepository = employeeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    // ------------------------------------------------------------------ reading

    public async Task<GetWorkflowSetupGridOutput> GetGrid()
    {
        var today = Clock.Now.Date;

        var types = await _transactionTypeRepository.GetAll()
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Code)
            .ToListAsync();

        var masters = await _requestMasterRepository.GetAll().ToListAsync();
        var details = await _requestDetailRepository.GetAll().ToListAsync();
        var roles = await _workflowRoleRepository.GetAll().ToListAsync();

        var holderCounts = (await _employeeWorkflowRoleRepository.GetAll()
                .Where(r => r.StartDate <= today && r.EndDate >= today)
                .Select(r => new { r.WorkflowRoleId, r.EmployeeId })
                .ToListAsync())
            .GroupBy(r => r.WorkflowRoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.EmployeeId).Distinct().Count());

        var parallelDetailIds = (await _requestDetailParallelRepository.GetAll()
                .Select(p => p.RequestDetailId)
                .ToListAsync())
            .ToHashSet();

        var groupChainTypeNames = (await _workflowGroupDetailRepository.GetAll()
                .Select(g => g.RequestTypeName)
                .ToListAsync())
            .Where(n => !n.IsNullOrWhiteSpace())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groupChainMasterIds = (await _workflowGroupDetailRepository.GetAll()
                .Select(g => g.RequestMasterId)
                .ToListAsync())
            .ToHashSet();

        // A master used by more than one transaction cannot be rewritten for one of them.
        var masterUseCounts = types
            .Where(t => t.DefaultRequestMasterId.HasValue)
            .GroupBy(t => t.DefaultRequestMasterId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var employeesWithoutManager = await CountEmployeesWithoutManagerAsync(today);

        var defaultMaster = masters.FirstOrDefault(m => IsDefaultChain(m));

        var defaultSteps = defaultMaster == null
            ? new List<RequestDetail>()
            : details.Where(d => d.RequestMasterId == defaultMaster.Id && d.ApproverPerson != WorkflowApproverPersonType.None)
                .OrderBy(d => d.Step)
                .ToList();

        var output = new GetWorkflowSetupGridOutput
        {
            WorkflowGroupsEnabled = groupChainTypeNames.Count > 0,
            DefaultChainUsedByCount = defaultMaster == null
                ? 0
                : types.Count(t => t.DefaultRequestMasterId == defaultMaster.Id),
            DefaultChainSteps = defaultSteps.Select(s => ToStepDto(s, roles, holderCounts)).ToList(),
            DefaultChainNotifyByEmail = defaultSteps.Any(s => s.NotificationConfigId.HasValue),
            DefaultChainEscalateAfterDays = defaultSteps
                .Where(s => s.ShouldEscalate)
                .Select(s => s.EscalateAfterDays)
                .DefaultIfEmpty(0)
                .Max(),
            Roles = roles
                .OrderBy(r => r.NameEn)
                .Select(r => new WorkflowSetupRoleDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    NameEn = r.NameEn,
                    NameAr = r.NameAr,
                    CurrentHolderCount = holderCounts.TryGetValue(r.Id, out var c) ? c : 0
                })
                .ToList()
        };

        foreach (var type in types)
        {
            var master = type.DefaultRequestMasterId.HasValue
                ? masters.FirstOrDefault(m => m.Id == type.DefaultRequestMasterId.Value)
                : null;

            var chainSteps = master == null
                ? new List<RequestDetail>()
                : details.Where(d => d.RequestMasterId == master.Id).OrderBy(d => d.Step).ToList();

            // A leading step with no approver is the creator row: it says WHO MAY RAISE this, not who
            // decides it. It is a column of its own in the grid, not one of the approver cells.
            var creatorStep = chainSteps.FirstOrDefault(s => s.ApproverPerson == WorkflowApproverPersonType.None);
            var approverSteps = chainSteps.Where(s => s.ApproverPerson != WorkflowApproverPersonType.None).ToList();

            var row = new WorkflowSetupRowDto
            {
                Code = type.Code,
                NameEn = type.NameEn,
                NameAr = type.NameAr,
                DisplayOrder = type.DisplayOrder,
                IsWorkflowEnabled = type.IsWorkflowEnabled,
                IsEssEnabled = type.IsEssEnabled,
                AllowDirectEntry = type.AllowDirectEntry,
                IsActive = type.IsActive,
                UsesDefaultChain = IsDefaultChain(master),
                RaisedBy = creatorStep?.Person ?? WorkflowPersonScope.None,
                NotifyByEmail = approverSteps.Any(s => s.NotificationConfigId.HasValue),
                EscalateAfterDays = approverSteps
                    .Where(s => s.ShouldEscalate)
                    .Select(s => s.EscalateAfterDays)
                    .DefaultIfEmpty(0)
                    .Max(),
                Steps = approverSteps.Select(s => new WorkflowSetupStepDto
                {
                    ApproverPerson = s.ApproverPerson,
                    RequestPersonField = s.RequestPersonField,
                    WorkflowRoleId = s.WorkflowRoleId,
                    WorkflowRoleName = s.WorkflowRoleId.HasValue
                        ? roles.FirstOrDefault(r => r.Id == s.WorkflowRoleId.Value)?.NameEn
                        : null,
                    ApprovalRequire = s.ApprovalRequire,
                    CurrentHolderCount = s.WorkflowRoleId.HasValue && holderCounts.TryGetValue(s.WorkflowRoleId.Value, out var hc)
                        ? hc
                        : 0
                }).ToList()
            };

            row.LockedReason = DescribeLock(
                type, master, approverSteps, chainSteps, parallelDetailIds, groupChainMasterIds, masterUseCounts);

            row.IsLocked = row.LockedReason != null;

            AddWarnings(row, type, groupChainTypeNames, employeesWithoutManager);

            output.Rows.Add(row);
        }

        return output;
    }

    /// <summary>
    /// Why this row cannot be edited from the grid, or null when it can.
    ///
    /// <para>
    /// Every reason here is a shape the grid has no column for. Saving over one would silently delete a
    /// working configuration, and the person doing it would have no way to know -- which is worse than a
    /// row they have to open the detailed screens to change.
    /// </para>
    /// </summary>
    private string DescribeLock(
        WorkflowTransactionType type,
        RequestMaster master,
        List<RequestDetail> approverSteps,
        List<RequestDetail> allSteps,
        HashSet<int> parallelDetailIds,
        HashSet<int> groupChainMasterIds,
        Dictionary<int, int> masterUseCounts)
    {
        if (master == null)
        {
            // Nothing configured yet. The most editable state there is.
            return null;
        }

        // The default chain is shared BY DESIGN, so sharing is not a reason to lock a row that follows
        // it. Everything else shared is somebody else's chain and rewriting it would change them too.
        var isDefaultChain = IsDefaultChain(master);

        if (!isDefaultChain && masterUseCounts.TryGetValue(master.Id, out var uses) && uses > 1)
        {
            return L("WorkflowSetupLockedSharedChain", master.NameEn, uses);
        }

        if (groupChainMasterIds.Contains(master.Id))
        {
            return L("WorkflowSetupLockedChainUsedByGroup", master.NameEn);
        }

        if (approverSteps.Count > MaxSteps)
        {
            return L("WorkflowSetupLockedTooManySteps", approverSteps.Count, MaxSteps);
        }

        if (allSteps.Any(s => parallelDetailIds.Contains(s.Id)))
        {
            return L("WorkflowSetupLockedParallelApprovers");
        }

        // A chain the grid did not create may carry per-step settings the grid has no column for, and
        // rewriting its steps would drop them. Anything the grid made is its own to rewrite.
        var isGridOwned = isDefaultChain
                          || (master.Code != null
                              && master.Code.Equals(GridChainPrefix + type.Code, StringComparison.OrdinalIgnoreCase));

        if (!isGridOwned && allSteps.Any(HasSettingsTheGridCannotShow))
        {
            return L("WorkflowSetupLockedAdvancedStepSettings", master.NameEn);
        }

        return null;
    }


    /// <summary>One step as the grid shows it. Shared by the rows and by the default chain panel.</summary>
    private static WorkflowSetupStepDto ToStepDto(
        RequestDetail step,
        List<WorkflowRole> roles,
        Dictionary<int, int> holderCounts)
    {
        return new WorkflowSetupStepDto
        {
            ApproverPerson = step.ApproverPerson,
            RequestPersonField = step.RequestPersonField,
            WorkflowRoleId = step.WorkflowRoleId,
            WorkflowRoleName = step.WorkflowRoleId.HasValue
                ? roles.FirstOrDefault(r => r.Id == step.WorkflowRoleId.Value)?.NameEn
                : null,
            ApprovalRequire = step.ApprovalRequire,
            CurrentHolderCount = step.WorkflowRoleId.HasValue && holderCounts.TryGetValue(step.WorkflowRoleId.Value, out var hc)
                ? hc
                : 0
        };
    }
    private static bool IsDefaultChain(RequestMaster master)
    {
        return master?.Code != null && master.Code.Equals(DefaultChainCode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Per-step settings with no column in the grid. Present means a rewrite would lose them.
    ///
    /// <para>
    /// Escalation is absent from this list on purpose -- the grid has a column for it. So are the
    /// notification slots the grid's own email switch writes.
    /// </para>
    /// </summary>
    private static bool HasSettingsTheGridCannotShow(RequestDetail step)
    {
        return step.SignatoryStep
               || step.EnableReadLog
               || step.ShowPayment
               || step.IsCommentRequire
               || step.ReminderDays > 0
               || step.WorkflowRoleOrgId.HasValue
               || step.WorkflowRoleGroup != WorkflowRoleGroup.None
               || step.EscalateWorkflowRoleId.HasValue
               || step.SubmitNotificationConfigId.HasValue
               || step.RejectNotificationConfigId.HasValue
               || step.SendBackNotificationConfigId.HasValue
               || step.RemarksNotificationConfigId.HasValue
               || step.AssignNotificationConfigId.HasValue
               || step.ReminderNotificationConfigId.HasValue;
    }

    /// <summary>
    /// Things worth saying that do not stop an edit. Each one is a configuration that saves cleanly and
    /// then fails at the first real request, which is the expensive way to find out.
    /// </summary>
    private void AddWarnings(
        WorkflowSetupRowDto row,
        WorkflowTransactionType type,
        HashSet<string> groupChainTypeNames,
        int employeesWithoutManager)
    {
        if (row.IsWorkflowEnabled && row.Steps.Count == 0)
        {
            row.Warnings.Add(L("WorkflowSetupWarningEnabledWithNoApprovers"));
        }

        foreach (var step in row.Steps)
        {
            if (step.ApproverPerson == WorkflowApproverPersonType.Person && !step.WorkflowRoleId.HasValue)
            {
                row.Warnings.Add(L("WorkflowSetupWarningPersonWithoutRole"));
            }
            else if (step.ApproverPerson == WorkflowApproverPersonType.Person && step.CurrentHolderCount == 0)
            {
                row.Warnings.Add(L("WorkflowSetupWarningRoleHasNobody", step.WorkflowRoleName));
            }
        }

        if (employeesWithoutManager > 0 && row.Steps.Any(s => IsManagerPosition(s.ApproverPerson)))
        {
            row.Warnings.Add(L("WorkflowSetupWarningEmployeesWithoutManager", employeesWithoutManager));
        }

        if (groupChainTypeNames.Contains(type.Code))
        {
            // Not a lock: the group's own chain is a separate row and editing the default leaves it alone.
            // But it does mean what is set here is not what some employees will get.
            row.Warnings.Add(L("WorkflowSetupWarningGroupChainOverrides"));
        }

        if (row.AllowDirectEntry && !row.IsWorkflowEnabled)
        {
            row.Warnings.Add(L("WorkflowSetupWarningDirectEntryWithoutWorkflow"));
        }
    }

    private static bool IsManagerPosition(WorkflowApproverPersonType approverPerson)
    {
        return approverPerson != WorkflowApproverPersonType.None
               && approverPerson != WorkflowApproverPersonType.Person;
    }

    private async Task<int> CountEmployeesWithoutManagerAsync(DateTime today)
    {
        var withManager = _employeeManagerRepository.GetAll()
            .Where(m => m.StartDate <= today && m.EndDate >= today && m.ManagerId != m.EmployeeId)
            .Select(m => m.EmployeeId);

        return await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Where(e => !withManager.Contains(e.Id))
            .CountAsync();
    }

    // ------------------------------------------------------------------ writing

    public async Task SaveRow(SaveWorkflowSetupRowInput input)
    {
        var type = await _transactionTypeRepository.FirstOrDefaultAsync(t => t.Code == input.Code);

        if (type == null)
        {
            throw new UserFriendlyException(L("WorkflowTransactionTypeNotRegistered", input.Code));
        }

        await EnsureRowIsEditableAsync(type);

        EnsureNoGapsBetweenApprovers(input.Steps);

        var steps = (input.Steps ?? new List<WorkflowSetupStepDto>())
            .Where(s => s.ApproverPerson != WorkflowApproverPersonType.None)
            .Take(MaxSteps)
            .ToList();

        RequestMaster master;

        if (input.UseDefaultChain)
        {
            // The default decides the approvers, so whatever the row sent for them is ignored rather than
            // written somewhere it would never be read.
            master = await RequireDefaultChainAsync();
        }
        else
        {
            ValidateSteps(input, steps);

            master = steps.Count == 0 && input.RaisedBy == WorkflowPersonScope.None
                ? null
                : await EnsureChainAsync(type, input, steps);
        }

        type.IsWorkflowEnabled = input.IsWorkflowEnabled;
        type.IsEssEnabled = input.IsEssEnabled;
        type.AllowDirectEntry = input.AllowDirectEntry;
        type.IsActive = input.IsActive;

        if (master != null)
        {
            type.DefaultRequestMasterId = master.Id;
        }

        await _transactionTypeRepository.UpdateAsync(type);
    }

    /// <summary>
    /// Refuses a save that would flatten something the grid cannot express. Re-derived here rather than
    /// trusted from the row the browser sent, because the lock is the whole safety story.
    /// </summary>
    private async Task EnsureRowIsEditableAsync(WorkflowTransactionType type)
    {
        var grid = await GetGrid();

        var row = grid.Rows.FirstOrDefault(r => r.Code == type.Code);

        if (row != null && row.IsLocked)
        {
            throw new UserFriendlyException(row.LockedReason);
        }
    }


    /// <summary>
    /// Refuses a route with a hole in it -- approver 3 set while approver 2 is blank.
    ///
    /// <para>
    /// Checked on the RAW slots, before the blanks are filtered out, because filtering is exactly what
    /// hides the mistake: the gap closes silently and the route saves with the approvers one position
    /// earlier than they were typed. Somebody setting a two-step route leaves the last slot blank on
    /// purpose; somebody leaving the MIDDLE one blank has made an error, and closing it for them means
    /// the screen and the saved route no longer agree.
    /// </para>
    /// </summary>
    private void EnsureNoGapsBetweenApprovers(List<WorkflowSetupStepDto> rawSteps)
    {
        if (rawSteps == null)
        {
            return;
        }

        var seenBlank = false;

        for (var i = 0; i < rawSteps.Count && i < MaxSteps; i++)
        {
            if (rawSteps[i].ApproverPerson == WorkflowApproverPersonType.None)
            {
                seenBlank = true;
                continue;
            }

            if (seenBlank)
            {
                throw new UserFriendlyException(L("WorkflowSetupApproversMustBeConsecutive", i + 1));
            }
        }
    }
    private void ValidateSteps(SaveWorkflowSetupRowInput input, List<WorkflowSetupStepDto> steps)
    {
        foreach (var step in steps)
        {
            if (step.ApproverPerson == WorkflowApproverPersonType.Person && !step.WorkflowRoleId.HasValue)
            {
                throw new UserFriendlyException(L("WorkflowSetupPersonNeedsARole"));
            }
        }

        // Enabling routing with nothing to route to fails at the first request rather than here, and the
        // person who set it would not connect the two. The catalog screen refuses the same combination.
        if (input.IsWorkflowEnabled && steps.Count == 0)
        {
            throw new UserFriendlyException(L("WorkflowSetupCannotEnableWithoutApprovers"));
        }

        if (input.EscalateAfterDays < 0)
        {
            throw new UserFriendlyException(L("WorkflowSetupEscalationDaysCannotBeNegative"));
        }
    }

    /// <summary>
    /// Creates or updates the chain this transaction owns, and replaces its steps outright.
    ///
    /// <para>
    /// Replacing rather than merging is deliberate: the grid shows the whole chain, so what is on screen is
    /// the whole intent. Merging would leave steps nobody can see from here.
    /// </para>
    /// </summary>
    private async Task<RequestMaster> EnsureChainAsync(
        WorkflowTransactionType type,
        SaveWorkflowSetupRowInput input,
        List<WorkflowSetupStepDto> steps)
    {
        var code = GridChainPrefix + type.Code;

        var master = await _requestMasterRepository.FirstOrDefaultAsync(m => m.Code == code);

        if (master == null)
        {
            master = new RequestMaster
            {
                TenantId = AbpSession.TenantId ?? 0,
                Code = code,
                NameEn = type.NameEn,
                NameAr = type.NameAr
            };

            master.Id = await _requestMasterRepository.InsertAndGetIdAsync(master);
        }

        var hasCreatorRow = input.RaisedBy != WorkflowPersonScope.None;

        master.NameEn = type.NameEn;
        master.NameAr = type.NameAr;
        master.WorkflowSteps = steps.Count + (hasCreatorRow ? 1 : 0);

        await _requestMasterRepository.UpdateAsync(master);
        await CurrentUnitOfWork.SaveChangesAsync();

        await ReplaceStepsAsync(master.Id, steps, input.RaisedBy, input.NotifyByEmail, input.EscalateAfterDays);

        return master;
    }

    /// <summary>
    /// Replaces a chain's steps outright, creator row included.
    ///
    /// <para>
    /// Shared by a transaction's own chain and by the default one, because they are the same thing written
    /// to a different master -- and two copies of this would be two places for the step numbering to drift.
    /// </para>
    /// </summary>
    private async Task ReplaceStepsAsync(
        int masterId,
        List<WorkflowSetupStepDto> steps,
        WorkflowPersonScope raisedBy,
        bool notifyByEmail,
        int escalateAfterDays)
    {
        var existing = await _requestDetailRepository.GetAllListAsync(d => d.RequestMasterId == masterId);

        foreach (var detail in existing)
        {
            await _requestDetailRepository.DeleteAsync(detail);
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        var notificationConfigId = notifyByEmail ? await EnsureNotificationConfigIdAsync() : (int?)null;

        var hasCreatorRow = raisedBy != WorkflowPersonScope.None;
        var stepNumber = 1;

        if (hasCreatorRow)
        {
            // ApproverPerson None with a Person scope is the engine's creator row: it names who may raise
            // this, and the approvals start at the step after it.
            await _requestDetailRepository.InsertAsync(new RequestDetail
            {
                TenantId = AbpSession.TenantId ?? 0,
                RequestMasterId = masterId,
                Step = stepNumber++,
                ApproverPerson = WorkflowApproverPersonType.None,
                Person = raisedBy,
                ApprovalRequire = ApprovalRequire.Single
            });
        }

        // A "Person on Request" step picked in the grid has no field to route by, and would be skipped for every
        // request. Refused here, so the administrator is sent to the step screen rather than left with a dead step.
        if (steps.Any(s => s.ApproverPerson == WorkflowApproverPersonType.PersonOnRequest && s.RequestPersonField.IsNullOrWhiteSpace()))
        {
            throw new UserFriendlyException(L("RequestPersonFieldChooseOnStepScreen"));
        }

        foreach (var step in steps)
        {
            await _requestDetailRepository.InsertAsync(new RequestDetail
            {
                TenantId = AbpSession.TenantId ?? 0,
                RequestMasterId = masterId,
                Step = stepNumber++,
                ApproverPerson = step.ApproverPerson,

                // Carried through unchanged: the grid shows a "Person on Request" step but its field is chosen on the
                // detailed step screen. See the check above the loop.
                RequestPersonField = step.ApproverPerson == WorkflowApproverPersonType.PersonOnRequest
                    ? step.RequestPersonField
                    : null,

                // Only meaningful for Person. A manager position carries no role, and leaving one on it
                // would show in the detailed screens as a setting that does nothing.
                WorkflowRoleId = step.ApproverPerson == WorkflowApproverPersonType.Person
                    ? step.WorkflowRoleId
                    : null,

                ApprovalRequire = step.ApproverPerson == WorkflowApproverPersonType.Person
                    ? step.ApprovalRequire
                    : ApprovalRequire.Single,

                // Sensible for every chain the grid makes, and both are what an approver expects to be able
                // to do. The detailed screens can still turn them off per step.
                CanReject = true,
                CanSendBack = true,

                ShouldEscalate = escalateAfterDays > 0,
                EscalateAfterDays = escalateAfterDays,
                EscalateNextLevel = escalateAfterDays > 0,

                NotificationConfigId = notificationConfigId
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();


    }

    /// <summary>
    /// The one notification setting the grid's email switch points at, created on first use.
    /// </summary>
    private async Task<int> EnsureNotificationConfigIdAsync()
    {
        var config = await _notificationConfigRepository
            .FirstOrDefaultAsync(c => c.Name == GridNotificationConfigName);

        if (config != null)
        {
            return config.Id;
        }

        return await _notificationConfigRepository.InsertAndGetIdAsync(new NotificationConfig
        {
            TenantId = AbpSession.TenantId ?? 0,
            Name = GridNotificationConfigName,
            SendNotification = true,
            SendEmail = true,

            // The person whose turn it is. Telling the requester their own request moved is a different
            // setting, and switching it on here would mail everybody on every step.
            SendToApprover = true
        });
    }


    public async Task SaveDefaultChain(SaveWorkflowSetupDefaultChainInput input)
    {
        EnsureNoGapsBetweenApprovers(input.Steps);

        var steps = (input.Steps ?? new List<WorkflowSetupStepDto>())
            .Where(s => s.ApproverPerson != WorkflowApproverPersonType.None)
            .Take(MaxSteps)
            .ToList();

        foreach (var step in steps)
        {
            if (step.ApproverPerson == WorkflowApproverPersonType.Person && !step.WorkflowRoleId.HasValue)
            {
                throw new UserFriendlyException(L("WorkflowSetupPersonNeedsARole"));
            }
        }

        if (input.EscalateAfterDays < 0)
        {
            throw new UserFriendlyException(L("WorkflowSetupEscalationDaysCannotBeNegative"));
        }

        var master = await _requestMasterRepository.FirstOrDefaultAsync(m => m.Code == DefaultChainCode);

        // Emptying the default would leave every transaction following it routing to nobody, which fails
        // at the first request rather than here. Refused while anything still uses it; the rows have to be
        // moved off it first, deliberately, one at a time.
        if (steps.Count == 0)
        {
            var followers = master == null
                ? 0
                : await _transactionTypeRepository.CountAsync(t => t.DefaultRequestMasterId == master.Id);

            if (followers > 0)
            {
                throw new UserFriendlyException(L("WorkflowSetupCannotEmptyDefaultChainInUse", followers));
            }
        }

        if (master == null)
        {
            master = new RequestMaster
            {
                TenantId = AbpSession.TenantId ?? 0,
                Code = DefaultChainCode,
                NameEn = "Default approval chain",
                NameAr = "مسار الاعتماد الافتراضي"
            };

            master.Id = await _requestMasterRepository.InsertAndGetIdAsync(master);
        }

        master.WorkflowSteps = steps.Count;

        await _requestMasterRepository.UpdateAsync(master);
        await CurrentUnitOfWork.SaveChangesAsync();

        // No creator row on the default: who may RAISE a transaction is a property of that transaction,
        // not of the chain it happens to share. Each row keeps its own answer.
        await ReplaceStepsAsync(
            master.Id, steps, WorkflowPersonScope.None, input.NotifyByEmail, input.EscalateAfterDays);
    }

    /// <summary>Creates the default chain's master on demand, for a row asking to follow it.</summary>
    private async Task<RequestMaster> RequireDefaultChainAsync()
    {
        var master = await _requestMasterRepository.FirstOrDefaultAsync(m => m.Code == DefaultChainCode);

        if (master == null)
        {
            throw new UserFriendlyException(L("WorkflowSetupDefineTheDefaultChainFirst"));
        }

        var hasSteps = await _requestDetailRepository
            .CountAsync(d => d.RequestMasterId == master.Id && d.ApproverPerson != WorkflowApproverPersonType.None);

        if (hasSteps == 0)
        {
            throw new UserFriendlyException(L("WorkflowSetupDefineTheDefaultChainFirst"));
        }

        return master;
    }
    // ------------------------------------------------------------------ roles

    public async Task<WorkflowSetupRoleDto> CreateRole(CreateWorkflowSetupRoleInput input)
    {
        var duplicate = await _workflowRoleRepository.GetAll().AnyAsync(r => r.Code == input.Code);

        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var role = new WorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,

            // The same scoping the Workflow Roles screen offers, so a role created here is not a
            // second-class one that has to be opened elsewhere to finish.
            WorkflowRoleGroup = input.WorkflowRoleGroup,
            WorkflowRoleOrgId = input.WorkflowRoleOrgId
        };

        role.Id = await _workflowRoleRepository.InsertAndGetIdAsync(role);

        await CurrentUnitOfWork.SaveChangesAsync();

        var employeeIds = (input.EmployeeIds ?? new List<int>()).Distinct().ToList();

        foreach (var employeeId in employeeIds)
        {
            await _employeeWorkflowRoleRepository.InsertAsync(new EmployeeWorkflowRole
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                WorkflowRoleId = role.Id,

                // From today, open-ended. A role created to be used now should resolve now; the detailed
                // screen is where a fixed term gets set.
                StartDate = Clock.Now.Date,
                EndDate = new DateTime(9999, 12, 31)
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return new WorkflowSetupRoleDto
        {
            Id = role.Id,
            Code = role.Code,
            NameEn = role.NameEn,
            NameAr = role.NameAr,
            CurrentHolderCount = employeeIds.Count
        };
    }

    public async Task<List<WorkflowSetupRoleMemberDto>> GetRoleMembers(int workflowRoleId)
    {
        var today = Clock.Now.Date;

        var query = from assignment in _employeeWorkflowRoleRepository.GetAll()
                    join employee in _employeeRepository.GetAll() on assignment.EmployeeId equals employee.Id
                    where assignment.WorkflowRoleId == workflowRoleId
                          && assignment.StartDate <= today
                          && assignment.EndDate >= today
                    select new WorkflowSetupRoleMemberDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.Code,
                        EmployeeNameEn = employee.FullNameEn,
                        EmployeeNameAr = employee.FullNameAr
                    };

        return await query.ToListAsync();
    }
}
