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
/// Application service for <see cref="ManageOrgSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so <see cref="DeleteOrgSettings"/>
/// always throws here too. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_OrgSettings)]
public class ManageOrgSettingsAppService : HCMv3AppServiceBase, IManageOrgSettingsAppService
{
    private readonly IRepository<ManageOrgSettings> _repository;

    public ManageOrgSettingsAppService(IRepository<ManageOrgSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<OrgSettingsListDto>> GetOrgSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrgSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<OrgSettingsEditDto> GetOrgSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditOrgSettings(OrgSettingsEditDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_OrgSettings_Delete)]
    public Task DeleteOrgSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_OrgSettings_Create)]
    protected virtual async Task CreateAsync(OrgSettingsEditDto input)
    {
        var entity = new ManageOrgSettings
        {
            EnablePostingGroup = input.EnablePostingGroup,
            EnableBudgetingGroup = input.EnableBudgetingGroup,
            EnablePosition = input.EnablePosition,
            EnableOrgChart3Levels = input.EnableOrgChart3Levels,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_OrgSettings_Edit)]
    protected virtual async Task UpdateAsync(OrgSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.EnablePostingGroup = input.EnablePostingGroup;
        entity.EnableBudgetingGroup = input.EnableBudgetingGroup;
        entity.EnablePosition = input.EnablePosition;
        entity.EnableOrgChart3Levels = input.EnableOrgChart3Levels;

        await _repository.UpdateAsync(entity);
    }

    private static OrgSettingsListDto ToListDto(ManageOrgSettings entity)
    {
        return new OrgSettingsListDto
        {
            Id = entity.Id,
            EnablePostingGroup = entity.EnablePostingGroup,
            EnableBudgetingGroup = entity.EnableBudgetingGroup,
            EnablePosition = entity.EnablePosition,
            EnableOrgChart3Levels = entity.EnableOrgChart3Levels
        };
    }

    private static OrgSettingsEditDto ToEditDto(ManageOrgSettings entity)
    {
        return new OrgSettingsEditDto
        {
            Id = entity.Id,
            EnablePostingGroup = entity.EnablePostingGroup,
            EnableBudgetingGroup = entity.EnableBudgetingGroup,
            EnablePosition = entity.EnablePosition,
            EnableOrgChart3Levels = entity.EnableOrgChart3Levels
        };
    }
}
