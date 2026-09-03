using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The only entry point for <see cref="EmployeeAssignedTask"/> (ported from legacy XAF
/// <c>TaskRequest</c>) -- assigning a piece of work to an employee and following it to Closed.
///
/// <para>
/// Creation routes through <see cref="IAdminTransactionSubmitter"/>, not through an ESS service, because a
/// task is ASSIGNED rather than asked for: legacy keeps the creator (<c>SysCreator</c>) and the subject
/// (<c>Employee</c>) as different people on purpose. The submitter therefore takes the employee from the
/// ENTITY. With workflow switched off for the type it simply inserts the row, which is the pre-workflow
/// behaviour.
/// </para>
///
/// <para>
/// <b>The creator/assignee rule is enforced here, on the server.</b> Legacy expressed it as XAF
/// <c>[Appearance(Enabled = false, Criteria = "IsCreator = 0")]</c> on Subject, Description, StartDate and
/// EndDate -- a UI rule on one detail view, which greyed four boxes out and stopped nothing: any other
/// screen, any import, any direct save went straight past it. Ported as a real check in
/// <see cref="UpdateAsync"/>: once the task is in the approval chain, only the person who assigned it may
/// change what the job is or when it runs. The assignee can still move <c>TaskStatus</c> (and add comments),
/// which is the whole reason they have the record open.
/// </para>
///
/// <para>
/// Legacy's <c>OnSaving</c> is not otherwise reproduced. It never called <c>base.OnSaving()</c>, so its save
/// pipeline never completed; it rethrew with <c>throw ex</c>, destroying the stack trace; and its request
/// numbering and approver assignment are the engine's job here, not the entity's. The validation rules it
/// stated are kept -- on the DTO, in <see cref="EnsureTaskIsValid"/> so HR is told what is wrong before the
/// row exists, and again in <see cref="AssignedTaskWorkflowTransactionHandler"/> for anything that reaches
/// the engine by another route.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks)]
public class EmployeeAssignedTasksAppService : HCMv3AppServiceBase, IEmployeeAssignedTasksAppService
{
    private readonly IRepository<EmployeeAssignedTask> _repository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeAssignedTasksAppService(
        IRepository<EmployeeAssignedTask> repository,
        IRepository<WorkflowRequest> requestRepository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _requestRepository = requestRepository;
        _transactionSubmitter = transactionSubmitter;
    }

    /// <summary>
    /// [HttpPost] because the input is a custom DTO: ABP would otherwise route this as a GET and NSwag would
    /// flatten the input into query-string parameters, never emitting the DTO class for the clients.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeeAssignedTaskDto>> GetAll(GetAllEmployeeAssignedTasksInput input)
    {
        input.Normalize();

        // The management screen sees tasks still awaiting approval as well as live ones. Knowing that an
        // assignment has been proposed is the point of the list; hiding it would invite a duplicate
        // assignment of the same work.
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.TaskStatus.HasValue, e => e.TaskStatus == input.TaskStatus.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e => e.Subject.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAssignedTaskDto>(totalCount, items.Select(ToDto).ToList());
    }

    /// <summary>
    /// Takes a required id (never a nullable one): a form opened for edit is editing something specific, and
    /// answering an absent id with an empty DTO hides a broken caller.
    /// </summary>
    public async Task<GetEmployeeAssignedTaskForEditOutput> GetEmployeeAssignedTaskForEdit(EntityDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAssignedTaskForEditOutput
        {
            EmployeeAssignedTask = new CreateOrEditEmployeeAssignedTaskDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                Subject = entity.Subject,
                Description = entity.Description,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                TaskStatus = entity.TaskStatus,
                Comments = entity.Comments
            },

            // Told to the form up front so it can disable the fields, rather than letting the user type and
            // then refusing the save. The refusal still happens on save regardless -- this flag is a courtesy,
            // not the control.
            CanEditTaskDetails = await CanEditTaskDetailsAsync(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAssignedTaskDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAssignedTaskDto input)
    {
        var entity = MapToEntity(input, new EmployeeAssignedTask());
        entity.TenantId = AbpSession.TenantId ?? 0;

        EnsureTaskIsValid(entity);

        // Routed for approval when the catalog says so, and simply recorded when it does not. No sub-type:
        // legacy routed every task through request code "17" regardless of what the work was.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            AssignedTaskWorkflowTransactionHandler.AssignedTaskRequestType,
            "TSK");
    }

    /// <summary>
    /// Applies an edit, subject to the creator/assignee rule.
    ///
    /// <para>
    /// Once the task has entered the approval chain, the four fields that define the JOB -- subject,
    /// description, start and end date -- may only be changed by whoever assigned it. Anyone else, the
    /// assignee included, may move the status and leave comments and nothing more. Before it enters the
    /// chain (workflow off for the type, or the request still at step 0) everything is open, exactly as
    /// legacy's <c>IsCreator</c> returned 1 while <c>WorkflowStep &lt;= 0</c>.
    /// </para>
    /// <para>
    /// One deliberate difference from legacy: legacy ALSO capped the creator at <c>WorkflowStep &lt;= 1</c>,
    /// so past the second step nobody at all could correct a typo in the subject. That cap is not ported --
    /// the rule that matters is who may change the work, not for how long -- and it is stated here so the
    /// difference is not mistaken for an oversight.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAssignedTaskDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id.Value);

        if (!await CanEditTaskDetailsAsync(entity) && ChangesTaskDetails(input, entity))
        {
            throw new UserFriendlyException(L("OnlyTheTaskCreatorCanChangeTaskDetails"));
        }

        MapToEntity(input, entity);

        EnsureTaskIsValid(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAssignedTasks_Delete)]
    public async Task Delete(EntityDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Whether the current user may change what the job is. True for the creator always, and for anybody
    /// while the task has not yet entered the chain.
    /// </summary>
    private async Task<bool> CanEditTaskDetailsAsync(EmployeeAssignedTask entity)
    {
        if (entity.CreatorUserId.HasValue && entity.CreatorUserId == AbpSession.UserId)
        {
            return true;
        }

        return !await IsInWorkflowChainAsync(entity);
    }

    /// <summary>
    /// Whether the task is past the first workflow step -- i.e. it has a request and that request has moved
    /// off step 0 into somebody's inbox. A task whose type is not routed has no request at all and is never
    /// in a chain, which is what keeps the rule from locking a workflow-free installation out of its own
    /// records.
    /// </summary>
    private async Task<bool> IsInWorkflowChainAsync(EmployeeAssignedTask entity)
    {
        if (!entity.WorkflowRequestId.HasValue)
        {
            return false;
        }

        var step = await _requestRepository.GetAll()
            .Where(r => r.Id == entity.WorkflowRequestId.Value)
            .Select(r => (int?)r.WorkflowStep)
            .FirstOrDefaultAsync();

        return step.HasValue && step.Value > 0;
    }

    private static bool ChangesTaskDetails(CreateOrEditEmployeeAssignedTaskDto input, EmployeeAssignedTask entity)
    {
        return input.Subject != entity.Subject
               || input.Description != entity.Description
               || input.StartDate.Date != entity.StartDate.Date
               || input.EndDate.Date != entity.EndDate.Date;
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c>, rule for rule -- subject, both dates, and end not before start --
    /// but raising <see cref="UserFriendlyException"/> with localized keys instead of raw
    /// <c>Exception</c>s carrying hardcoded English, and without legacy's
    /// <c>catch (Exception ex) { throw ex; }</c>, which reset the stack trace of every failure to the
    /// rethrow line.
    /// </summary>
    private void EnsureTaskIsValid(EmployeeAssignedTask entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (entity.Subject.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("TaskSubjectIsRequired"));
        }

        if (entity.StartDate == default)
        {
            throw new UserFriendlyException(L("StartDateIsRequired"));
        }

        if (entity.EndDate == default)
        {
            throw new UserFriendlyException(L("EndDateIsRequired"));
        }

        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeAssignedTask MapToEntity(CreateOrEditEmployeeAssignedTaskDto input, EmployeeAssignedTask entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Subject = input.Subject;
        entity.Description = input.Description;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.TaskStatus = input.TaskStatus;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAssignedTaskDto ToDto(EmployeeAssignedTask entity)
    {
        return new EmployeeAssignedTaskDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Subject = entity.Subject,
            Description = entity.Description,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TaskStatus = entity.TaskStatus,
            Comments = entity.Comments,
            CreatorUserId = entity.CreatorUserId,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }
}
