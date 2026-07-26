using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="ExecutionLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.ExecutionLog -- see entity remarks for the dropped
/// DocAttachment FK simplification). Plain audit-log CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_ExecutionLogs)]
public class ExecutionLogsAppService : HCMv3AppServiceBase, IExecutionLogsAppService
{
    private readonly IRepository<ExecutionLog> _repository;

    public ExecutionLogsAppService(IRepository<ExecutionLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ExecutionLogDto>> GetAll(GetAllExecutionLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.Module.HasValue, l => l.Module == input.Module);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ExecutionLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetExecutionLogForEditOutput> GetExecutionLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetExecutionLogForEditOutput
        {
            ExecutionLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditExecutionLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditExecutionLogDto input)
    {
        var entity = MapToEntity(input, new ExecutionLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditExecutionLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ExecutionLog MapToEntity(CreateOrEditExecutionLogDto input, ExecutionLog entity)
    {
        entity.UserName = input.UserName;
        entity.ActionDateTime = input.ActionDateTime;
        entity.Action = input.Action;
        entity.Module = input.Module;
        entity.ScreenName = input.ScreenName;
        entity.ScreenCaption = input.ScreenCaption;
        entity.Parameters = input.Parameters;
        entity.ExecutionStatus = input.ExecutionStatus;
        entity.TotalRecords = input.TotalRecords;
        entity.Successfull = input.Successfull;
        entity.Failed = input.Failed;

        return entity;
    }

    private static ExecutionLogDto ToDto(ExecutionLog entity)
    {
        return new ExecutionLogDto
        {
            Id = entity.Id,
            UserName = entity.UserName,
            ActionDateTime = entity.ActionDateTime,
            Action = entity.Action,
            Module = entity.Module,
            ScreenName = entity.ScreenName,
            ScreenCaption = entity.ScreenCaption,
            Parameters = entity.Parameters,
            ExecutionStatus = entity.ExecutionStatus,
            TotalRecords = entity.TotalRecords,
            Successfull = entity.Successfull,
            Failed = entity.Failed
        };
    }

    private static CreateOrEditExecutionLogDto ToCreateOrEditDto(ExecutionLog entity)
    {
        return new CreateOrEditExecutionLogDto
        {
            Id = entity.Id,
            UserName = entity.UserName,
            ActionDateTime = entity.ActionDateTime,
            Action = entity.Action,
            Module = entity.Module,
            ScreenName = entity.ScreenName,
            ScreenCaption = entity.ScreenCaption,
            Parameters = entity.Parameters,
            ExecutionStatus = entity.ExecutionStatus,
            TotalRecords = entity.TotalRecords,
            Successfull = entity.Successfull,
            Failed = entity.Failed
        };
    }
}
