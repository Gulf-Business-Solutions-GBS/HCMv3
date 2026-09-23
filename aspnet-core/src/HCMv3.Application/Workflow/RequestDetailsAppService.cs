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
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetail</c>).
///
/// STEP NUMBERING (legacy <c>PerformValidation</c>/<c>AddStep</c>/<c>OnDeleted</c>):
/// <list type="bullet">
/// <item>A new step always goes to the end of the chain, whatever step number the screen sent
/// (legacy <c>AddStep(count)</c>).</item>
/// <item>Changing an existing step's number is governed by <c>ManageWorkflowSettings.DynamicWorkflowSteps</c>:
/// off, and the number is put back to what it was (legacy <c>this.Step = this.prevStep</c>); on, and the
/// step moves to the requested position with the steps in between shifting to make room, after a 1..count
/// range check.</item>
/// <item>After every create, move or delete the chain is renumbered 1..n and
/// <see cref="RequestMaster.WorkflowSteps"/> is set to n.</item>
/// </list>
/// Plus the legacy Reminder/Escalate day-ordering check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails)]
public class RequestDetailsAppService : HCMv3AppServiceBase, IRequestDetailsAppService
{
    private readonly IRepository<RequestDetail> _repository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public RequestDetailsAppService(
        IRepository<RequestDetail> repository,
        IRepository<RequestMaster> requestMasterRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _requestMasterRepository = requestMasterRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestDetailDto>> GetAll(GetAllRequestDetailsInput input)
    {
        var query = _repository.GetAll()
            .Include(d => d.WorkflowRole)
            .WhereIf(input.RequestMasterId.HasValue, d => d.RequestMasterId == input.RequestMasterId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Step" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetRequestDetailForEditOutput> GetRequestDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetRequestDetailForEditOutput { RequestDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditRequestDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var requestMasterId = entity.RequestMasterId;

        await _repository.DeleteAsync(input.Id);

        await RecalculateWorkflowStepsAsync(requestMasterId);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRequestDetailDto input)
    {
        var entity = MapToEntity(input, new RequestDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateReminderVsEscalate(entity);

        // Legacy AddStep(count): a new step is appended, whatever number the screen sent.
        entity.Step = await _repository.GetAll().CountAsync(d => d.RequestMasterId == entity.RequestMasterId) + 1;

        await _repository.InsertAsync(entity);

        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousStep = entity.Step;
        entity = MapToEntity(input, entity);

        ValidateReminderVsEscalate(entity);

        if (entity.Step != previousStep)
        {
            var settings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();
            if (settings == null || !settings.DynamicWorkflowSteps)
            {
                // Legacy: this.Step = this.prevStep -- the number is not editable.
                entity.Step = previousStep;
            }
            else
            {
                await MoveStepAsync(entity, previousStep);
            }
        }

        await _repository.UpdateAsync(entity);

        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c>, <c>DynamicWorkflowSteps == true</c> branch: the step takes the
    /// requested position and every step between the old and the new position shifts one place to make room.
    /// </summary>
    private async Task MoveStepAsync(RequestDetail entity, int previousStep)
    {
        var count = await _repository.GetAll().CountAsync(d => d.RequestMasterId == entity.RequestMasterId);
        if (entity.Step < 1 || entity.Step > count)
        {
            throw new UserFriendlyException(L("RequestDetailStepOutOfRange", count));
        }

        var siblings = await _repository.GetAll()
            .Where(d => d.RequestMasterId == entity.RequestMasterId && d.Id != entity.Id)
            .ToListAsync();

        foreach (var sibling in siblings)
        {
            if (previousStep > entity.Step && sibling.Step >= entity.Step && sibling.Step < previousStep)
            {
                sibling.Step += 1;
            }
            else if (previousStep < entity.Step && sibling.Step > previousStep && sibling.Step <= entity.Step)
            {
                sibling.Step -= 1;
            }
        }
    }

    private void ValidateReminderVsEscalate(RequestDetail entity)
    {
        if (entity.EscalateAfterDays != 0 && entity.ReminderDays >= entity.EscalateAfterDays)
        {
            throw new UserFriendlyException(L("ReminderDaysMustBeLessThanEscalateAfterDays"));
        }
    }

    private async Task RecalculateWorkflowStepsAsync(int requestMasterId)
    {
        // The pending insert/delete MUST be flushed first. CountAsync issues a SQL COUNT, which does not see
        // rows EF Core is still holding in the change tracker, so without this the count is always one
        // behind: zero after the first step is added, one after the second, and so on.
        //
        // That is not cosmetic. WorkflowApprovalService reads WorkflowSteps as the total number of steps, so
        // an undercount ends the approval chain early -- on a two-step chain the request would be marked
        // fully approved the moment the FIRST approver acted, and the second approver would never be asked.
        await CurrentUnitOfWork.SaveChangesAsync();

        // Legacy AddStep / OnDeleted: the chain is always numbered 1..n with no gaps.
        var details = await _repository.GetAll()
            .Where(d => d.RequestMasterId == requestMasterId)
            .OrderBy(d => d.Step)
            .ThenBy(d => d.Id)
            .ToListAsync();

        for (var i = 0; i < details.Count; i++)
        {
            if (details[i].Step != i + 1)
            {
                details[i].Step = i + 1;
            }
        }

        var requestMaster = await _requestMasterRepository.GetAsync(requestMasterId);
        requestMaster.WorkflowSteps = details.Count;
        await _requestMasterRepository.UpdateAsync(requestMaster);
    }

    private static RequestDetail MapToEntity(CreateOrEditRequestDetailDto input, RequestDetail entity)
    {
        entity.RequestMasterId = input.RequestMasterId;
        entity.Step = input.Step;
        entity.WorkflowRoleId = input.WorkflowRoleId;
        entity.ShowPayment = input.ShowPayment;
        entity.CanSendBack = input.CanSendBack;
        entity.CanReject = input.CanReject;
        entity.ShouldEscalate = input.ShouldEscalate;
        entity.EscalateAfterDays = input.EscalateAfterDays;
        entity.EscalateNextLevel = input.EscalateNextLevel;
        entity.EscalateWorkflowRoleId = input.EscalateWorkflowRoleId;
        entity.EscalateWorkflowRoleGroup = input.EscalateWorkflowRoleGroup;
        entity.EscalateWorkflowRoleOrgId = input.EscalateWorkflowRoleOrgId;
        entity.EnableReadLog = input.EnableReadLog;
        entity.Person = input.Person;
        entity.ApproverPerson = input.ApproverPerson;
        entity.WorkflowRoleGroup = input.WorkflowRoleGroup;
        entity.WorkflowRoleOrgId = input.WorkflowRoleOrgId;
        entity.ApprovalRequire = input.ApprovalRequire;
        entity.IsCommentRequire = input.IsCommentRequire;
        entity.SignatoryStep = input.SignatoryStep;
        entity.ReminderDays = input.ReminderDays;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.SubmitNotificationConfigId = input.SubmitNotificationConfigId;
        entity.RejectNotificationConfigId = input.RejectNotificationConfigId;
        entity.SendBackNotificationConfigId = input.SendBackNotificationConfigId;
        entity.RemarksNotificationConfigId = input.RemarksNotificationConfigId;
        entity.AssignNotificationConfigId = input.AssignNotificationConfigId;
        entity.ReminderNotificationConfigId = input.ReminderNotificationConfigId;
        return entity;
    }

    private static RequestDetailDto ToDto(RequestDetail entity)
    {
        return new RequestDetailDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            Step = entity.Step,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleNameEn = entity.WorkflowRole?.NameEn,
            ShowPayment = entity.ShowPayment,
            CanSendBack = entity.CanSendBack,
            CanReject = entity.CanReject,
            ShouldEscalate = entity.ShouldEscalate,
            EscalateAfterDays = entity.EscalateAfterDays,
            EscalateNextLevel = entity.EscalateNextLevel,
            EscalateWorkflowRoleId = entity.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = entity.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = entity.EscalateWorkflowRoleOrgId,
            EnableReadLog = entity.EnableReadLog,
            Person = entity.Person,
            ApproverPerson = entity.ApproverPerson,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId,
            ApprovalRequire = entity.ApprovalRequire,
            IsCommentRequire = entity.IsCommentRequire,
            SignatoryStep = entity.SignatoryStep,
            ReminderDays = entity.ReminderDays,
            NotificationConfigId = entity.NotificationConfigId,
            SubmitNotificationConfigId = entity.SubmitNotificationConfigId,
            RejectNotificationConfigId = entity.RejectNotificationConfigId,
            SendBackNotificationConfigId = entity.SendBackNotificationConfigId,
            RemarksNotificationConfigId = entity.RemarksNotificationConfigId,
            AssignNotificationConfigId = entity.AssignNotificationConfigId,
            ReminderNotificationConfigId = entity.ReminderNotificationConfigId
        };
    }

    private static CreateOrEditRequestDetailDto ToCreateOrEditDto(RequestDetail entity)
    {
        return new CreateOrEditRequestDetailDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            Step = entity.Step,
            WorkflowRoleId = entity.WorkflowRoleId,
            ShowPayment = entity.ShowPayment,
            CanSendBack = entity.CanSendBack,
            CanReject = entity.CanReject,
            ShouldEscalate = entity.ShouldEscalate,
            EscalateAfterDays = entity.EscalateAfterDays,
            EscalateNextLevel = entity.EscalateNextLevel,
            EscalateWorkflowRoleId = entity.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = entity.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = entity.EscalateWorkflowRoleOrgId,
            EnableReadLog = entity.EnableReadLog,
            Person = entity.Person,
            ApproverPerson = entity.ApproverPerson,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId,
            ApprovalRequire = entity.ApprovalRequire,
            IsCommentRequire = entity.IsCommentRequire,
            SignatoryStep = entity.SignatoryStep,
            ReminderDays = entity.ReminderDays,
            NotificationConfigId = entity.NotificationConfigId,
            SubmitNotificationConfigId = entity.SubmitNotificationConfigId,
            RejectNotificationConfigId = entity.RejectNotificationConfigId,
            SendBackNotificationConfigId = entity.SendBackNotificationConfigId,
            RemarksNotificationConfigId = entity.RemarksNotificationConfigId,
            AssignNotificationConfigId = entity.AssignNotificationConfigId,
            ReminderNotificationConfigId = entity.ReminderNotificationConfigId
        };
    }
}
