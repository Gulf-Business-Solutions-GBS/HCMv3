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
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManageNotificationSettings"/>. Legacy <c>OnDeleting</c>
/// unconditionally throws ("You can't delete record. Please contact administrator."), so
/// <see cref="DeleteNotificationSettings"/> always throws here too. Legacy <c>PerformValidation</c> was
/// empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_NotificationSettings)]
public class ManageNotificationSettingsAppService : HCMv3AppServiceBase, IManageNotificationSettingsAppService
{
    private readonly IRepository<ManageNotificationSettings> _repository;

    public ManageNotificationSettingsAppService(IRepository<ManageNotificationSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<NotificationSettingsListDto>> GetNotificationSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NotificationSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<NotificationSettingsEditDto> GetNotificationSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditNotificationSettings(NotificationSettingsEditDto input)
    {
        if (input.Id != 0)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_NotificationSettings_Delete)]
    public Task DeleteNotificationSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_NotificationSettings_Create)]
    protected virtual async Task CreateAsync(NotificationSettingsEditDto input)
    {
        var entity = new ManageNotificationSettings
        {
            DisableNotificationWindow = input.DisableNotificationWindow,
            AdvancePaymentReminderDateType = input.AdvancePaymentReminderDateType,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_NotificationSettings_Edit)]
    protected virtual async Task UpdateAsync(NotificationSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.DisableNotificationWindow = input.DisableNotificationWindow;
        entity.AdvancePaymentReminderDateType = input.AdvancePaymentReminderDateType;

        await _repository.UpdateAsync(entity);
    }

    private static NotificationSettingsListDto ToListDto(ManageNotificationSettings entity)
    {
        return new NotificationSettingsListDto
        {
            Id = entity.Id,
            DisableNotificationWindow = entity.DisableNotificationWindow,
            AdvancePaymentReminderDateType = entity.AdvancePaymentReminderDateType
        };
    }

    private static NotificationSettingsEditDto ToEditDto(ManageNotificationSettings entity)
    {
        return new NotificationSettingsEditDto
        {
            Id = entity.Id,
            DisableNotificationWindow = entity.DisableNotificationWindow,
            AdvancePaymentReminderDateType = entity.AdvancePaymentReminderDateType
        };
    }
}
