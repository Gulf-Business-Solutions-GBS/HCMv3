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
/// Application service for <see cref="TaskEvent"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.Util.TaskEvent -- see entity remarks for the documented simplifications).
/// Plain CRUD; the postpone/alarm popup machinery dropped from the entity has no server-side counterpart.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_TaskEvents)]
public class TaskEventsAppService : HCMv3AppServiceBase, ITaskEventsAppService
{
    private readonly IRepository<TaskEvent> _repository;

    public TaskEventsAppService(IRepository<TaskEvent> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TaskEventDto>> GetAll(GetAllTaskEventsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.RefEmployee)
            .Include(t => t.NotificationConfig)
            .WhereIf(input.RefEmployeeId.HasValue, t => t.RefEmployeeId == input.RefEmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TaskEventDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTaskEventForEditOutput> GetTaskEventForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTaskEventForEditOutput
        {
            TaskEvent = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTaskEventDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_TaskEvents_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_TaskEvents_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTaskEventDto input)
    {
        var entity = MapToEntity(input, new TaskEvent());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_TaskEvents_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTaskEventDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static TaskEvent MapToEntity(CreateOrEditTaskEventDto input, TaskEvent entity)
    {
        entity.RefEmployeeId = input.RefEmployeeId;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.Subject = input.Subject;
        entity.Description = input.Description;
        entity.DueDate = input.DueDate;
        entity.StartDate = input.StartDate;
        entity.Status = input.Status;
        entity.PercentCompleted = input.PercentCompleted;
        entity.DateCompleted = input.DateCompleted;
        entity.RemindIn = input.RemindIn;
        entity.AlarmTime = input.AlarmTime;
        entity.ObjectType = input.ObjectType;
        entity.ObjectOId = input.ObjectOId;
        return entity;
    }

    private static TaskEventDto ToDto(TaskEvent entity)
    {
        return new TaskEventDto
        {
            Id = entity.Id,
            RefEmployeeId = entity.RefEmployeeId,
            RefEmployeeFullNameEn = entity.RefEmployee?.FullNameEn,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            Subject = entity.Subject,
            Description = entity.Description,
            DueDate = entity.DueDate,
            StartDate = entity.StartDate,
            Status = entity.Status,
            PercentCompleted = entity.PercentCompleted,
            DateCompleted = entity.DateCompleted,
            RemindIn = entity.RemindIn,
            AlarmTime = entity.AlarmTime,
            ObjectType = entity.ObjectType,
            ObjectOId = entity.ObjectOId
        };
    }

    private static CreateOrEditTaskEventDto ToCreateOrEditDto(TaskEvent entity)
    {
        return new CreateOrEditTaskEventDto
        {
            Id = entity.Id,
            RefEmployeeId = entity.RefEmployeeId,
            NotificationConfigId = entity.NotificationConfigId,
            Subject = entity.Subject,
            Description = entity.Description,
            DueDate = entity.DueDate,
            StartDate = entity.StartDate,
            Status = entity.Status,
            PercentCompleted = entity.PercentCompleted,
            DateCompleted = entity.DateCompleted,
            RemindIn = entity.RemindIn,
            AlarmTime = entity.AlarmTime,
            ObjectType = entity.ObjectType,
            ObjectOId = entity.ObjectOId
        };
    }
}
