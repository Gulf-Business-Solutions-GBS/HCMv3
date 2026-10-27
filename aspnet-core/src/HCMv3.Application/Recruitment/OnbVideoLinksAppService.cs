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
/// Application service for <see cref="OnbVideoLink"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbVideoLink</c>). Legacy <c>PerformValidation</c> was
/// commented out, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbVideoLinks)]
public class OnbVideoLinksAppService : HCMv3AppServiceBase, IOnbVideoLinksAppService
{
    private readonly IRepository<OnbVideoLink> _onbVideoLinkRepository;

    public OnbVideoLinksAppService(IRepository<OnbVideoLink> onbVideoLinkRepository)
    {
        _onbVideoLinkRepository = onbVideoLinkRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbVideoLinkDto>> GetAll(GetAllOnbVideoLinksInput input)
    {
        input.Normalize();

        var query = _onbVideoLinkRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), v =>
                v.NameEn.Contains(input.Filter) ||
                v.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbVideoLinkDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbVideoLinkForEditOutput> GetOnbVideoLinkForEdit(EntityDto input)
    {
        var entity = await _onbVideoLinkRepository.GetAsync(input.Id);

        return new GetOnbVideoLinkForEditOutput
        {
            OnbVideoLink = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbVideoLinkDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbVideoLinks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbVideoLinkRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbVideoLinks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbVideoLinkDto input)
    {
        var entity = MapToEntity(input, new OnbVideoLink());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbVideoLinkRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbVideoLinks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbVideoLinkDto input)
    {
        var entity = await _onbVideoLinkRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbVideoLinkRepository.UpdateAsync(entity);
    }

    private static OnbVideoLink MapToEntity(CreateOrEditOnbVideoLinkDto input, OnbVideoLink entity)
    {
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.Link = input.Link;
        entity.IsActive = input.IsActive;

        return entity;
    }

    private static OnbVideoLinkDto ToDto(OnbVideoLink entity)
    {
        return new OnbVideoLinkDto
        {
            Id = entity.Id,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Link = entity.Link,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditOnbVideoLinkDto ToCreateOrEditDto(OnbVideoLink entity)
    {
        return new CreateOrEditOnbVideoLinkDto
        {
            Id = entity.Id,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Link = entity.Link,
            IsActive = entity.IsActive
        };
    }
}
