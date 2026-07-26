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
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="NotificationMaster"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.NotificationMaster -- see entity remarks for the documented
/// simplifications). Legacy PerformValidation parity: NotificationType required, unique per
/// (NotificationType).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters)]
public class NotificationMastersAppService : HCMv3AppServiceBase, INotificationMastersAppService
{
    private readonly IRepository<NotificationMaster> _repository;

    public NotificationMastersAppService(IRepository<NotificationMaster> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NotificationMasterDto>> GetAll(GetAllNotificationMastersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(m => m.NotificationConfig)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), m => m.NotificationType.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NotificationMasterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetNotificationMasterForEditOutput> GetNotificationMasterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNotificationMasterForEditOutput
        {
            NotificationMaster = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNotificationMasterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNotificationMasterDto input)
    {
        var entity = MapToEntity(input, new NotificationMaster());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNotificationMasterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(NotificationMaster entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(m => m.Id != entity.Id && m.NotificationType == entity.NotificationType);
        if (duplicate)
        {
            throw new UserFriendlyException(L("NotificationTypeAlreadyAssigned"));
        }
    }

    private static NotificationMaster MapToEntity(CreateOrEditNotificationMasterDto input, NotificationMaster entity)
    {
        entity.NotificationType = input.NotificationType;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.Criterion = input.Criterion;
        entity.IsEnabled = input.IsEnabled;

        return entity;
    }

    private static NotificationMasterDto ToDto(NotificationMaster entity)
    {
        return new NotificationMasterDto
        {
            Id = entity.Id,
            NotificationType = entity.NotificationType,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            Criterion = entity.Criterion,
            IsEnabled = entity.IsEnabled
        };
    }

    private static CreateOrEditNotificationMasterDto ToCreateOrEditDto(NotificationMaster entity)
    {
        return new CreateOrEditNotificationMasterDto
        {
            Id = entity.Id,
            NotificationType = entity.NotificationType,
            NotificationConfigId = entity.NotificationConfigId,
            Criterion = entity.Criterion,
            IsEnabled = entity.IsEnabled
        };
    }
}
