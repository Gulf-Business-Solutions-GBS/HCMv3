using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="OnbGroupDetail"/>, an aggregated child of <see cref="OnbGroup"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.OnbGroupDetail</c>). Legacy had no
/// <c>PerformValidation</c> override, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroupDetails)]
public class OnbGroupDetailsAppService : HCMv3AppServiceBase, IOnbGroupDetailsAppService
{
    private readonly IRepository<OnbGroupDetail> _onbGroupDetailRepository;

    public OnbGroupDetailsAppService(IRepository<OnbGroupDetail> onbGroupDetailRepository)
    {
        _onbGroupDetailRepository = onbGroupDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbGroupDetailDto>> GetAll(GetAllOnbGroupDetailsInput input)
    {
        input.Normalize();

        var query = _onbGroupDetailRepository.GetAll()
            .Include(d => d.OnbTask)
            .Include(d => d.JobTitle)
            .WhereIf(input.OnbGroupId.HasValue, d => d.OnbGroupId == input.OnbGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbGroupDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbGroupDetailForEditOutput> GetOnbGroupDetailForEdit(EntityDto input)
    {
        var entity = await _onbGroupDetailRepository.GetAsync(input.Id);

        return new GetOnbGroupDetailForEditOutput
        {
            OnbGroupDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbGroupDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroupDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbGroupDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroupDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbGroupDetailDto input)
    {
        var entity = MapToEntity(input, new OnbGroupDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbGroupDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbGroupDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbGroupDetailDto input)
    {
        var entity = await _onbGroupDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbGroupDetailRepository.UpdateAsync(entity);
    }

    private static OnbGroupDetail MapToEntity(CreateOrEditOnbGroupDetailDto input, OnbGroupDetail entity)
    {
        entity.OnbGroupId = input.OnbGroupId;
        entity.OnbTaskId = input.OnbTaskId;
        entity.JobTitleId = input.JobTitleId;

        return entity;
    }

    private static OnbGroupDetailDto ToDto(OnbGroupDetail entity)
    {
        return new OnbGroupDetailDto
        {
            Id = entity.Id,
            OnbGroupId = entity.OnbGroupId,
            OnbTaskId = entity.OnbTaskId,
            OnbTaskNameEn = entity.OnbTask?.TaskNameEn,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn
        };
    }

    private static CreateOrEditOnbGroupDetailDto ToCreateOrEditDto(OnbGroupDetail entity)
    {
        return new CreateOrEditOnbGroupDetailDto
        {
            Id = entity.Id,
            OnbGroupId = entity.OnbGroupId,
            OnbTaskId = entity.OnbTaskId,
            JobTitleId = entity.JobTitleId
        };
    }
}
