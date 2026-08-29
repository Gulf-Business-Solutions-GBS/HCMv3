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
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetail</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy entity's elaborate dynamic step-renumbering/shifting logic
/// (<c>PerformValidation</c>/<c>AddStep</c>/<c>OnDeleted</c>, driven by a <c>DynamicWorkflowSteps</c>
/// system setting) is not reproduced verbatim; this service enforces the simpler invariant that
/// <see cref="RequestMaster.WorkflowSteps"/> is always kept in sync with the actual child row count
/// after Create/Update/Delete, plus the legacy Reminder/Escalate day-ordering check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails)]
public class RequestDetailsAppService : HCMv3AppServiceBase, IRequestDetailsAppService
{
    private readonly IRepository<RequestDetail> _repository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;

    public RequestDetailsAppService(
        IRepository<RequestDetail> repository,
        IRepository<RequestMaster> requestMasterRepository)
    {
        _repository = repository;
        _requestMasterRepository = requestMasterRepository;
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

        await _repository.InsertAsync(entity);


        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateReminderVsEscalate(entity);

        await _repository.UpdateAsync(entity);

        await RecalculateWorkflowStepsAsync(entity.RequestMasterId);
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

        var requestMaster = await _requestMasterRepository.GetAsync(requestMasterId);
        requestMaster.WorkflowSteps = await _repository.GetAll().CountAsync(d => d.RequestMasterId == requestMasterId);
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
            ReminderDays = entity.ReminderDays
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
            ReminderDays = entity.ReminderDays
        };
    }
}
