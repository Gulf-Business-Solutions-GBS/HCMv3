using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestLog"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestLog</c>). Legacy UI made rows
/// non-editable after creation (<c>Appearance("EditDisabled", ...)</c>); reflected here by only
/// exposing Create (no Update).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestLogs)]
public class RequestLogsAppService : HCMv3AppServiceBase, IRequestLogsAppService
{
    private readonly IRepository<RequestLog> _repository;

    public RequestLogsAppService(IRepository<RequestLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestLogDto>> GetAll(GetAllRequestLogsInput input)
    {
        var query = _repository.GetAll()
            .Include(l => l.ActionBy)
            .WhereIf(input.WorkflowRequestId.HasValue, l => l.WorkflowRequestId == input.WorkflowRequestId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "ActionDateTime" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestLogs_Create)]
    public async Task Create(CreateRequestLogDto input)
    {
        var entity = new RequestLog
        {
            TenantId = AbpSession.TenantId ?? 0,
            WorkflowRequestId = input.WorkflowRequestId,
            ActionById = input.ActionById,
            RequestActionComment = input.RequestActionComment,
            ActionDateTime = DateTime.Now,
            ActionType = input.ActionType,
            WorkflowStep = input.WorkflowStep,
            ActionDays = input.ActionDays,
            Platform = input.Platform
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static RequestLogDto ToDto(RequestLog entity)
    {
        return new RequestLogDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            ActionById = entity.ActionById,
            ActionByNameEn = entity.ActionBy?.FirstNameEn,
            RequestActionComment = entity.RequestActionComment,
            ActionDateTime = entity.ActionDateTime,
            ActionType = entity.ActionType,
            WorkflowStep = entity.WorkflowStep,
            ActionDays = entity.ActionDays,
            Platform = entity.Platform
        };
    }
}
