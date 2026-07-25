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
/// Application service for <see cref="OnbTemplateDetails"/>, an aggregated child of
/// <see cref="OnbTemplate"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTemplateDetails</c>). Legacy <c>PerformValidation</c> was
/// an empty no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplateDetails)]
public class OnbTemplateDetailsAppService : HCMv3AppServiceBase, IOnbTemplateDetailsAppService
{
    private readonly IRepository<OnbTemplateDetails> _onbTemplateDetailsRepository;

    public OnbTemplateDetailsAppService(IRepository<OnbTemplateDetails> onbTemplateDetailsRepository)
    {
        _onbTemplateDetailsRepository = onbTemplateDetailsRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbTemplateDetailsDto>> GetAll(GetAllOnbTemplateDetailsInput input)
    {
        input.Normalize();

        var query = _onbTemplateDetailsRepository.GetAll()
            .WhereIf(input.OnbTemplateId.HasValue, d => d.OnbTemplateId == input.OnbTemplateId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbTemplateDetailsDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbTemplateDetailsForEditOutput> GetOnbTemplateDetailsForEdit(EntityDto input)
    {
        var entity = await _onbTemplateDetailsRepository.GetAsync(input.Id);

        return new GetOnbTemplateDetailsForEditOutput
        {
            OnbTemplateDetails = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbTemplateDetailsDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbTemplateDetailsRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbTemplateDetailsDto input)
    {
        var entity = MapToEntity(input, new OnbTemplateDetails());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbTemplateDetailsRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbTemplateDetailsDto input)
    {
        var entity = await _onbTemplateDetailsRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbTemplateDetailsRepository.UpdateAsync(entity);
    }

    private static OnbTemplateDetails MapToEntity(CreateOrEditOnbTemplateDetailsDto input, OnbTemplateDetails entity)
    {
        entity.OnbTemplateId = input.OnbTemplateId;
        entity.TemplateId = input.TemplateId;
        entity.TemplateData = input.TemplateData;

        return entity;
    }

    private static OnbTemplateDetailsDto ToDto(OnbTemplateDetails entity)
    {
        return new OnbTemplateDetailsDto
        {
            Id = entity.Id,
            OnbTemplateId = entity.OnbTemplateId,
            TemplateId = entity.TemplateId,
            TemplateData = entity.TemplateData
        };
    }

    private static CreateOrEditOnbTemplateDetailsDto ToCreateOrEditDto(OnbTemplateDetails entity)
    {
        return new CreateOrEditOnbTemplateDetailsDto
        {
            Id = entity.Id,
            OnbTemplateId = entity.OnbTemplateId,
            TemplateId = entity.TemplateId,
            TemplateData = entity.TemplateData
        };
    }
}
