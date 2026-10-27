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
/// Application service for <see cref="WFReadLog"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.WFReadLog</c>). Create-only (no Update), matching
/// the legacy read-only-audit-trail nature of this entity.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WFReadLogs)]
public class WFReadLogsAppService : HCMv3AppServiceBase, IWFReadLogsAppService
{
    private readonly IRepository<WFReadLog> _repository;

    public WFReadLogsAppService(IRepository<WFReadLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WFReadLogDto>> GetAll(GetAllWFReadLogsInput input)
    {
        var query = _repository.GetAll()
            .WhereIf(input.WorkflowRequestId.HasValue, l => l.WorkflowRequestId == input.WorkflowRequestId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "ReadDateTime" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WFReadLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WFReadLogs_Create)]
    public async Task Create(CreateWFReadLogDto input)
    {
        var entity = new WFReadLog
        {
            TenantId = AbpSession.TenantId ?? 0,
            WorkflowRequestId = input.WorkflowRequestId,
            ReadDateTime = DateTime.Now,
            User = input.User
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WFReadLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static WFReadLogDto ToDto(WFReadLog entity)
    {
        return new WFReadLogDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            ReadDateTime = entity.ReadDateTime,
            User = entity.User
        };
    }
}
