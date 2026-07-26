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
/// Application service for <see cref="ManageProjectCostingSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageProjectCostingSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- ported to <see cref="DeleteProjectCostingSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_ProjectCostingSettings)]
public class ManageProjectCostingSettingsAppService : HCMv3AppServiceBase, IManageProjectCostingSettingsAppService
{
    private readonly IRepository<ManageProjectCostingSettings> _repository;

    public ManageProjectCostingSettingsAppService(IRepository<ManageProjectCostingSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<ProjectCostingSettingsListDto>> GetProjectCostingSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectCostingSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<ProjectCostingSettingsEditDto> GetProjectCostingSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Create, AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Edit)]
    public async Task CreateOrEditProjectCostingSettings(ProjectCostingSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Delete)]
    public Task DeleteProjectCostingSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(ProjectCostingSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageProjectCostingSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(ProjectCostingSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageProjectCostingSettings MapToEntity(ProjectCostingSettingsEditDto input, ManageProjectCostingSettings entity)
    {
        entity.CalculateNetPayProjectWise = input.CalculateNetPayProjectWise;

        return entity;
    }

    private static ProjectCostingSettingsListDto ToListDto(ManageProjectCostingSettings entity)
    {
        return new ProjectCostingSettingsListDto
        {
            Id = entity.Id,
            CalculateNetPayProjectWise = entity.CalculateNetPayProjectWise
        };
    }

    private static ProjectCostingSettingsEditDto ToEditDto(ManageProjectCostingSettings entity)
    {
        return new ProjectCostingSettingsEditDto
        {
            Id = entity.Id,
            CalculateNetPayProjectWise = entity.CalculateNetPayProjectWise
        };
    }
}
