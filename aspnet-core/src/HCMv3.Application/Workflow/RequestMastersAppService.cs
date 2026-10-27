using System;
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
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestMaster"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestMaster</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters)]
public class RequestMastersAppService : HCMv3AppServiceBase, IRequestMastersAppService
{
    private readonly IRepository<RequestMaster> _repository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RequestDetailParallel> _requestDetailParallelRepository;

    public RequestMastersAppService(
        IRepository<RequestMaster> repository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RequestDetailParallel> requestDetailParallelRepository)
    {
        _repository = repository;
        _requestDetailRepository = requestDetailRepository;
        _requestDetailParallelRepository = requestDetailParallelRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestMasterDto>> GetAll(GetAllRequestMastersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestMasterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetRequestMasterForEditOutput> GetRequestMasterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetRequestMasterForEditOutput { RequestMaster = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditRequestMasterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Copies a whole chain: the header, every step with all of its settings, and each step's parallel
    /// approvers.
    ///
    /// <para>
    /// Chains for different transactions are usually the same few steps with one difference, so building
    /// the second from the first is the common case. Unlike the Administration tool
    /// (<c>WorkflowAdminToolsAppService.CopyWorkflowSetup</c>), which keeps legacy's selective field list,
    /// this copies everything: a copy that quietly drops the send-back, reject and notification settings
    /// has to be checked field by field, which is the work copying was meant to save.
    /// </para>
    ///
    /// <para>
    /// The copy gets a free code ("CODE-COPY", then "CODE-COPY2" ...) and "(Copy)" names, because code and
    /// both names must be unique. It is not attached to any transaction type or workflow group: routing
    /// requests to it is a separate, deliberate decision.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Create)]
    public async Task<EntityDto> Copy(EntityDto input)
    {
        var source = await _repository.GetAsync(input.Id);

        var sourceSteps = await _requestDetailRepository.GetAll()
            .Where(d => d.RequestMasterId == source.Id)
            .OrderBy(d => d.Step)
            .ToListAsync();

        var copy = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = await FreeCopyValueAsync(source.Code ?? string.Empty, "-COPY", SimpleLookupConsts.MaxCodeLength,
                candidate => _repository.GetAll().AnyAsync(r => r.Code == candidate)),
            NameEn = await FreeCopyValueAsync(source.NameEn, " (Copy)", SimpleLookupConsts.MaxNameLength,
                candidate => _repository.GetAll().AnyAsync(r => r.NameEn == candidate || r.NameAr == candidate)),
            NameAr = await FreeCopyValueAsync(source.NameAr, " (نسخة)", SimpleLookupConsts.MaxNameLength,
                candidate => _repository.GetAll().AnyAsync(r => r.NameEn == candidate || r.NameAr == candidate)),
            WorkflowSteps = sourceSteps.Count,
            SecurityLevel = source.SecurityLevel,
            AutoClose = source.AutoClose,
            AutoCloseInDays = source.AutoCloseInDays,
            DefaultApproverId = source.DefaultApproverId,
            IsCommentRequire = source.IsCommentRequire,
            DueInDays = source.DueInDays
        };

        var copyId = await _repository.InsertAndGetIdAsync(copy);

        foreach (var step in sourceSteps)
        {
            var stepCopyId = await _requestDetailRepository.InsertAndGetIdAsync(CopyStep(step, copyId, copy.TenantId));

            var parallels = await _requestDetailParallelRepository.GetAll()
                .Where(p => p.RequestDetailId == step.Id)
                .ToListAsync();

            foreach (var parallel in parallels)
            {
                await _requestDetailParallelRepository.InsertAsync(new RequestDetailParallel
                {
                    TenantId = copy.TenantId,
                    RequestDetailId = stepCopyId,
                    JobTitleId = parallel.JobTitleId,
                    WorkflowRoleId = parallel.WorkflowRoleId
                });
            }
        }

        return new EntityDto(copyId);
    }

    private static RequestDetail CopyStep(RequestDetail source, int requestMasterId, int tenantId)
    {
        return new RequestDetail
        {
            TenantId = tenantId,
            RequestMasterId = requestMasterId,
            Step = source.Step,
            WorkflowRoleId = source.WorkflowRoleId,
            ShowPayment = source.ShowPayment,
            CanSendBack = source.CanSendBack,
            CanReject = source.CanReject,
            ShouldEscalate = source.ShouldEscalate,
            EscalateAfterDays = source.EscalateAfterDays,
            EscalateNextLevel = source.EscalateNextLevel,
            EscalateWorkflowRoleId = source.EscalateWorkflowRoleId,
            EscalateWorkflowRoleGroup = source.EscalateWorkflowRoleGroup,
            EscalateWorkflowRoleOrgId = source.EscalateWorkflowRoleOrgId,
            EnableReadLog = source.EnableReadLog,
            Person = source.Person,
            ApproverPerson = source.ApproverPerson,
            WorkflowRoleGroup = source.WorkflowRoleGroup,
            WorkflowRoleOrgId = source.WorkflowRoleOrgId,
            ApprovalRequire = source.ApprovalRequire,
            IsCommentRequire = source.IsCommentRequire,
            SignatoryStep = source.SignatoryStep,
            ReminderDays = source.ReminderDays,
            NotificationConfigId = source.NotificationConfigId,
            SubmitNotificationConfigId = source.SubmitNotificationConfigId,
            RejectNotificationConfigId = source.RejectNotificationConfigId,
            SendBackNotificationConfigId = source.SendBackNotificationConfigId,
            RemarksNotificationConfigId = source.RemarksNotificationConfigId,
            AssignNotificationConfigId = source.AssignNotificationConfigId,
            ReminderNotificationConfigId = source.ReminderNotificationConfigId
        };
    }

    /// <summary>
    /// The value with the suffix appended, numbered from 2 until nothing else uses it, and trimmed so the
    /// suffix always survives the column's length limit.
    /// </summary>
    private static async Task<string> FreeCopyValueAsync(
        string value, string suffix, int maxLength, Func<string, Task<bool>> isTaken)
    {
        value ??= string.Empty;

        for (var n = 1; ; n++)
        {
            var fullSuffix = n == 1 ? suffix : suffix + n;
            var stem = value.Length + fullSuffix.Length > maxLength
                ? value.Substring(0, maxLength - fullSuffix.Length)
                : value;
            var candidate = stem + fullSuffix;

            if (!await isTaken(candidate))
            {
                return candidate;
            }
        }
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRequestMasterDto input)
    {
        var entity = MapToEntity(input, new RequestMaster());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestMasterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(RequestMaster entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(RequestMaster entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static RequestMaster MapToEntity(CreateOrEditRequestMasterDto input, RequestMaster entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.SecurityLevel = input.SecurityLevel;
        entity.AutoClose = input.AutoClose;
        entity.AutoCloseInDays = input.AutoCloseInDays;
        entity.DefaultApproverId = input.DefaultApproverId;
        entity.IsCommentRequire = input.IsCommentRequire;
        entity.DueInDays = input.DueInDays;
        return entity;
    }

    private static RequestMasterDto ToDto(RequestMaster entity)
    {
        return new RequestMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WorkflowSteps = entity.WorkflowSteps,
            SecurityLevel = entity.SecurityLevel,
            AutoClose = entity.AutoClose,
            AutoCloseInDays = entity.AutoCloseInDays,
            DefaultApproverId = entity.DefaultApproverId,
            IsCommentRequire = entity.IsCommentRequire,
            DueInDays = entity.DueInDays
        };
    }

    private static CreateOrEditRequestMasterDto ToCreateOrEditDto(RequestMaster entity)
    {
        return new CreateOrEditRequestMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            SecurityLevel = entity.SecurityLevel,
            AutoClose = entity.AutoClose,
            AutoCloseInDays = entity.AutoCloseInDays,
            DefaultApproverId = entity.DefaultApproverId,
            IsCommentRequire = entity.IsCommentRequire,
            DueInDays = entity.DueInDays
        };
    }
}
