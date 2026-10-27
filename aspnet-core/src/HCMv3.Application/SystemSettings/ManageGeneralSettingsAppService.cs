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
/// Application service for <see cref="ManageGeneralSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so <see cref="DeleteGeneralSettings"/>
/// always throws here too. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_GeneralSettings)]
public class ManageGeneralSettingsAppService : HCMv3AppServiceBase, IManageGeneralSettingsAppService
{
    private readonly IRepository<ManageGeneralSettings> _repository;

    public ManageGeneralSettingsAppService(IRepository<ManageGeneralSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<ManageGeneralSettingsListDto>> GetGeneralSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ManageGeneralSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<ManageGeneralSettingsEditDto> GetGeneralSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditGeneralSettings(ManageGeneralSettingsEditDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_GeneralSettings_Delete)]
    public Task DeleteGeneralSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_GeneralSettings_Create)]
    protected virtual async Task CreateAsync(ManageGeneralSettingsEditDto input)
    {
        var entity = new ManageGeneralSettings
        {
            DisableAutoEmpcode = input.DisableAutoEmpcode,
            DateEditMask = input.DateEditMask,
            DateDisplayFormat = input.DateDisplayFormat,
            TimeEditMask = input.TimeEditMask,
            TimeDisplayFormat = input.TimeDisplayFormat,
            StartupDashboard = input.StartupDashboard,
            NoticePeriodDaysExcludingSubmissionDate = input.NoticePeriodDaysExcludingSubmissionDate,
            EnableGridDataAccessModeServer = input.EnableGridDataAccessModeServer,
            StartHangfire = input.StartHangfire,
            AutoFilterColumnCondition = input.AutoFilterColumnCondition,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_GeneralSettings_Edit)]
    protected virtual async Task UpdateAsync(ManageGeneralSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.DisableAutoEmpcode = input.DisableAutoEmpcode;
        entity.DateEditMask = input.DateEditMask;
        entity.DateDisplayFormat = input.DateDisplayFormat;
        entity.TimeEditMask = input.TimeEditMask;
        entity.TimeDisplayFormat = input.TimeDisplayFormat;
        entity.StartupDashboard = input.StartupDashboard;
        entity.NoticePeriodDaysExcludingSubmissionDate = input.NoticePeriodDaysExcludingSubmissionDate;
        entity.EnableGridDataAccessModeServer = input.EnableGridDataAccessModeServer;
        entity.StartHangfire = input.StartHangfire;
        entity.AutoFilterColumnCondition = input.AutoFilterColumnCondition;

        await _repository.UpdateAsync(entity);
    }

    private static ManageGeneralSettingsListDto ToListDto(ManageGeneralSettings entity)
    {
        return new ManageGeneralSettingsListDto
        {
            Id = entity.Id,
            DisableAutoEmpcode = entity.DisableAutoEmpcode,
            DateEditMask = entity.DateEditMask,
            DateDisplayFormat = entity.DateDisplayFormat,
            TimeEditMask = entity.TimeEditMask,
            TimeDisplayFormat = entity.TimeDisplayFormat,
            StartupDashboard = entity.StartupDashboard,
            EnableGridDataAccessModeServer = entity.EnableGridDataAccessModeServer,
            StartHangfire = entity.StartHangfire
        };
    }

    private static ManageGeneralSettingsEditDto ToEditDto(ManageGeneralSettings entity)
    {
        return new ManageGeneralSettingsEditDto
        {
            Id = entity.Id,
            DisableAutoEmpcode = entity.DisableAutoEmpcode,
            DateEditMask = entity.DateEditMask,
            DateDisplayFormat = entity.DateDisplayFormat,
            TimeEditMask = entity.TimeEditMask,
            TimeDisplayFormat = entity.TimeDisplayFormat,
            StartupDashboard = entity.StartupDashboard,
            NoticePeriodDaysExcludingSubmissionDate = entity.NoticePeriodDaysExcludingSubmissionDate,
            EnableGridDataAccessModeServer = entity.EnableGridDataAccessModeServer,
            StartHangfire = entity.StartHangfire,
            AutoFilterColumnCondition = entity.AutoFilterColumnCondition
        };
    }
}
