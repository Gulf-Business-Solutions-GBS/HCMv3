using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="OnbGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbGroup</c>). Legacy <c>PerformValidation</c> was an empty
/// no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroups)]
public class OnbGroupsAppService : HCMv3AppServiceBase, IOnbGroupsAppService
{
    private readonly IRepository<OnbGroup> _onbGroupRepository;

    public OnbGroupsAppService(IRepository<OnbGroup> onbGroupRepository)
    {
        _onbGroupRepository = onbGroupRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbGroupDto>> GetAll(GetAllOnbGroupsInput input)
    {
        input.Normalize();

        var query = _onbGroupRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), g =>
                g.NameEn.Contains(input.Filter) ||
                g.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbGroupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbGroupForEditOutput> GetOnbGroupForEdit(EntityDto input)
    {
        var entity = await _onbGroupRepository.GetAsync(input.Id);

        return new GetOnbGroupForEditOutput
        {
            OnbGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbGroupRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbGroupDto input)
    {
        var entity = MapToEntity(input, new OnbGroup());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbGroupRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbGroupDto input)
    {
        var entity = await _onbGroupRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbGroupRepository.UpdateAsync(entity);
    }

    private static OnbGroup MapToEntity(CreateOrEditOnbGroupDto input, OnbGroup entity)
    {
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        return entity;
    }

    private static OnbGroupDto ToDto(OnbGroup entity)
    {
        return new OnbGroupDto
        {
            Id = entity.Id,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditOnbGroupDto ToCreateOrEditDto(OnbGroup entity)
    {
        return new CreateOrEditOnbGroupDto
        {
            Id = entity.Id,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
