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
/// Application service for <see cref="OnbTemplate"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTemplate</c>). Legacy <c>PerformValidation</c> was an
/// empty no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplates)]
public class OnbTemplatesAppService : HCMv3AppServiceBase, IOnbTemplatesAppService
{
    private readonly IRepository<OnbTemplate> _onbTemplateRepository;

    public OnbTemplatesAppService(IRepository<OnbTemplate> onbTemplateRepository)
    {
        _onbTemplateRepository = onbTemplateRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbTemplateDto>> GetAll(GetAllOnbTemplatesInput input)
    {
        input.Normalize();

        var query = _onbTemplateRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t => t.TemplateTitle.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbTemplateDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbTemplateForEditOutput> GetOnbTemplateForEdit(EntityDto input)
    {
        var entity = await _onbTemplateRepository.GetAsync(input.Id);

        return new GetOnbTemplateForEditOutput
        {
            OnbTemplate = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbTemplateDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplates_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbTemplateRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplates_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbTemplateDto input)
    {
        var entity = MapToEntity(input, new OnbTemplate());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbTemplateRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTemplates_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbTemplateDto input)
    {
        var entity = await _onbTemplateRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbTemplateRepository.UpdateAsync(entity);
    }

    private static OnbTemplate MapToEntity(CreateOrEditOnbTemplateDto input, OnbTemplate entity)
    {
        entity.TemplateId = input.TemplateId;
        entity.TemplateTitle = input.TemplateTitle;

        return entity;
    }

    private static OnbTemplateDto ToDto(OnbTemplate entity)
    {
        return new OnbTemplateDto
        {
            Id = entity.Id,
            TemplateId = entity.TemplateId,
            TemplateTitle = entity.TemplateTitle
        };
    }

    private static CreateOrEditOnbTemplateDto ToCreateOrEditDto(OnbTemplate entity)
    {
        return new CreateOrEditOnbTemplateDto
        {
            Id = entity.Id,
            TemplateId = entity.TemplateId,
            TemplateTitle = entity.TemplateTitle
        };
    }
}
