using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the NotificationConfig master (ported from legacy XAF NotificationConfig).
/// No Code/NameEn/NameAr per legacy (just Name): no auto-code or duplicate Code/Name validation.
/// The legacy <c>EmailTemplate</c> FK is not ported (module out of scope).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs)]
public class NotificationConfigsAppService : HCMv3AppServiceBase, INotificationConfigsAppService
{
    private readonly IRepository<NotificationConfig> _repository;

    public NotificationConfigsAppService(IRepository<NotificationConfig> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NotificationConfigDto>> GetAll(GetAllNotificationConfigsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c => c.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NotificationConfigDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetNotificationConfigForEditOutput> GetNotificationConfigForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNotificationConfigForEditOutput
        {
            NotificationConfig = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNotificationConfigDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNotificationConfigDto input)
    {
        var entity = new NotificationConfig
        {
            Name = input.Name,
            SendNotification = input.SendNotification,
            SendEmail = input.SendEmail,
            ToEmployee = input.ToEmployee,
            ToRequestCreator = input.ToRequestCreator,
            ToManager = input.ToManager,
            SendToAll = input.SendToAll,
            SendToAllApproved = input.SendToAllApproved,
            SendToApprover = input.SendToApprover,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNotificationConfigDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Name = input.Name;
        entity.SendNotification = input.SendNotification;
        entity.SendEmail = input.SendEmail;
        entity.ToEmployee = input.ToEmployee;
        entity.ToRequestCreator = input.ToRequestCreator;
        entity.ToManager = input.ToManager;
        entity.SendToAll = input.SendToAll;
        entity.SendToAllApproved = input.SendToAllApproved;
        entity.SendToApprover = input.SendToApprover;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static NotificationConfigDto ToDto(NotificationConfig entity)
    {
        return new NotificationConfigDto
        {
            Id = entity.Id,
            Name = entity.Name,
            SendNotification = entity.SendNotification,
            SendEmail = entity.SendEmail,
            ToEmployee = entity.ToEmployee,
            ToRequestCreator = entity.ToRequestCreator,
            ToManager = entity.ToManager,
            SendToAll = entity.SendToAll,
            SendToAllApproved = entity.SendToAllApproved,
            SendToApprover = entity.SendToApprover
        };
    }

    private static CreateOrEditNotificationConfigDto ToCreateOrEditDto(NotificationConfig entity)
    {
        return new CreateOrEditNotificationConfigDto
        {
            Id = entity.Id,
            Name = entity.Name,
            SendNotification = entity.SendNotification,
            SendEmail = entity.SendEmail,
            ToEmployee = entity.ToEmployee,
            ToRequestCreator = entity.ToRequestCreator,
            ToManager = entity.ToManager,
            SendToAll = entity.SendToAll,
            SendToAllApproved = entity.SendToAllApproved,
            SendToApprover = entity.SendToApprover
        };
    }
}
