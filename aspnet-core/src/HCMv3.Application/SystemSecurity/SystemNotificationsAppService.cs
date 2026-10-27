using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="SystemNotification"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.SystemNotification). Legacy PerformValidation was empty (no
/// business rules) -- ported as-is with plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_SystemNotifications)]
public class SystemNotificationsAppService : HCMv3AppServiceBase, ISystemNotificationsAppService
{
    private readonly IRepository<SystemNotification> _repository;

    public SystemNotificationsAppService(IRepository<SystemNotification> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SystemNotificationDto>> GetAll(GetAllSystemNotificationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(n => n.NotificationConfig)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), n => n.NotificationName.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SystemNotificationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSystemNotificationForEditOutput> GetSystemNotificationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSystemNotificationForEditOutput
        {
            SystemNotification = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSystemNotificationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SystemNotifications_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SystemNotifications_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSystemNotificationDto input)
    {
        var entity = MapToEntity(input, new SystemNotification());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SystemNotifications_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSystemNotificationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static SystemNotification MapToEntity(CreateOrEditSystemNotificationDto input, SystemNotification entity)
    {
        entity.NotificationName = input.NotificationName;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.ReminderDays = input.ReminderDays;
        entity.IsActive = input.IsActive;

        return entity;
    }

    private static SystemNotificationDto ToDto(SystemNotification entity)
    {
        return new SystemNotificationDto
        {
            Id = entity.Id,
            NotificationName = entity.NotificationName,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            ReminderDays = entity.ReminderDays,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditSystemNotificationDto ToCreateOrEditDto(SystemNotification entity)
    {
        return new CreateOrEditSystemNotificationDto
        {
            Id = entity.Id,
            NotificationName = entity.NotificationName,
            NotificationConfigId = entity.NotificationConfigId,
            ReminderDays = entity.ReminderDays,
            IsActive = entity.IsActive
        };
    }
}
