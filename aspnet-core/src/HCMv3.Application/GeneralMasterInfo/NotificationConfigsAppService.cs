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
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the NotificationConfig master (ported from legacy XAF NotificationConfig).
/// No Code/NameEn/NameAr per legacy (just Name): no auto-code or duplicate Code/Name validation.
/// The notification groups (role holders to tell) are saved with the configuration, as legacy edited them
/// inside it; the separate Notification Groups screen is no longer in the menu.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs)]
public class NotificationConfigsAppService : HCMv3AppServiceBase, INotificationConfigsAppService
{
    private readonly IRepository<NotificationConfig> _repository;
    private readonly IRepository<NotificationGroup> _groupRepository;

    public NotificationConfigsAppService(
        IRepository<NotificationConfig> repository,
        IRepository<NotificationGroup> groupRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
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

        var output = ToCreateOrEditDto(entity);
        output.Groups = await _groupRepository.GetAll()
            .Where(g => g.NotificationConfigId == entity.Id)
            .OrderBy(g => g.Id)
            .Select(g => new NotificationConfigGroupDto
            {
                WorkflowRoleId = g.WorkflowRoleId,
                WorkflowRoleGroup = g.WorkflowRoleGroup,
                WorkflowRoleOrgId = g.WorkflowRoleOrgId
            })
            .ToListAsync();

        return new GetNotificationConfigForEditOutput
        {
            NotificationConfig = output
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
            EmailTemplateId = input.EmailTemplateId,
            TenantId = AbpSession.TenantId ?? 0
        };

        Validate(input);

        var id = await _repository.InsertAndGetIdAsync(entity);
        await ReplaceGroupsAsync(id, entity.TenantId, input.Groups);
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
        entity.EmailTemplateId = input.EmailTemplateId;

        Validate(input);

        await _repository.UpdateAsync(entity);
        await ReplaceGroupsAsync(entity.Id, entity.TenantId, input.Groups);
    }

    /// <summary>
    /// Legacy <c>NotificationConfig.PerformValidation</c>: a name is required, and Send To All (every active
    /// employee) cannot be combined with Employee, Request Creator, Manager or notification groups -- those
    /// would only send the same people the notice twice.
    /// </summary>
    private void Validate(CreateOrEditNotificationConfigDto input)
    {
        if (input.Name.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("NameCannotBeBlank"));
        }

        if (input.SendToAll &&
            (input.ToEmployee || input.ToRequestCreator || input.ToManager || (input.Groups?.Count ?? 0) > 0))
        {
            throw new UserFriendlyException(L("UncheckSendToAllToSetOtherRecipients"));
        }
    }

    /// <summary>The groups sent replace the ones the configuration had.</summary>
    private async Task ReplaceGroupsAsync(int notificationConfigId, int tenantId, System.Collections.Generic.List<NotificationConfigGroupDto> groups)
    {
        await _groupRepository.DeleteAsync(g => g.NotificationConfigId == notificationConfigId);

        foreach (var group in groups ?? new System.Collections.Generic.List<NotificationConfigGroupDto>())
        {
            await _groupRepository.InsertAsync(new NotificationGroup
            {
                TenantId = tenantId,
                NotificationConfigId = notificationConfigId,
                WorkflowRoleId = group.WorkflowRoleId,
                WorkflowRoleGroup = group.WorkflowRoleGroup,
                WorkflowRoleOrgId = group.WorkflowRoleOrgId
            });
        }
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
            SendToApprover = entity.SendToApprover,
            EmailTemplateId = entity.EmailTemplateId
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
            SendToApprover = entity.SendToApprover,
            EmailTemplateId = entity.EmailTemplateId
        };
    }
}
