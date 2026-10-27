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
/// Application service for <see cref="SysTenantConfig"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.SysTenantConfig). Legacy PerformValidation was empty; EndDate
/// must not be earlier than StartDate is enforced here as a reasonable date-effective-range guard.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysTenantConfigs)]
public class SysTenantConfigsAppService : HCMv3AppServiceBase, ISysTenantConfigsAppService
{
    private readonly IRepository<SysTenantConfig> _repository;

    public SysTenantConfigsAppService(IRepository<SysTenantConfig> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SysTenantConfigDto>> GetAll(GetAllSysTenantConfigsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c => c.Key.Contains(input.Filter))
            .WhereIf(input.ConfigCategory.HasValue, c => c.ConfigCategory == input.ConfigCategory);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SysTenantConfigDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSysTenantConfigForEditOutput> GetSysTenantConfigForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSysTenantConfigForEditOutput
        {
            SysTenantConfig = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSysTenantConfigDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSysTenantConfigDto input)
    {
        var entity = MapToEntity(input, new SysTenantConfig());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDateRange(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSysTenantConfigDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDateRange(entity);

        await _repository.UpdateAsync(entity);
    }

    private void ValidateDateRange(SysTenantConfig entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static SysTenantConfig MapToEntity(CreateOrEditSysTenantConfigDto input, SysTenantConfig entity)
    {
        entity.Key = input.Key;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.KeyValue = input.KeyValue;
        entity.ConfigCategory = input.ConfigCategory;
        entity.ConfigDescription = input.ConfigDescription;

        return entity;
    }

    private static SysTenantConfigDto ToDto(SysTenantConfig entity)
    {
        return new SysTenantConfigDto
        {
            Id = entity.Id,
            Key = entity.Key,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            KeyValue = entity.KeyValue,
            ConfigCategory = entity.ConfigCategory,
            ConfigDescription = entity.ConfigDescription
        };
    }

    private static CreateOrEditSysTenantConfigDto ToCreateOrEditDto(SysTenantConfig entity)
    {
        return new CreateOrEditSysTenantConfigDto
        {
            Id = entity.Id,
            Key = entity.Key,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            KeyValue = entity.KeyValue,
            ConfigCategory = entity.ConfigCategory,
            ConfigDescription = entity.ConfigDescription
        };
    }
}
