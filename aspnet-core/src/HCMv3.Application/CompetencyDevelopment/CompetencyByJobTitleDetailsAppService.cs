using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for the weighted competency targets within a <see cref="CompetencyByJobTitle"/>
/// profile (ported from legacy XAF CompetencyByJobTitleDetail business object). See
/// <see cref="CompetencyByJobTitleDetail"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails)]
public class CompetencyByJobTitleDetailsAppService : HCMv3AppServiceBase, ICompetencyByJobTitleDetailsAppService
{
    private readonly IRepository<CompetencyByJobTitleDetail> _repository;

    public CompetencyByJobTitleDetailsAppService(IRepository<CompetencyByJobTitleDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CompetencyByJobTitleDetailDto>> GetAll(GetAllCompetencyByJobTitleDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Competency)
            .WhereIf(input.CompetencyByJobTitleId.HasValue, d => d.CompetencyByJobTitleId == input.CompetencyByJobTitleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CompetencyByJobTitleDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCompetencyByJobTitleDetailForEditOutput> GetCompetencyByJobTitleDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCompetencyByJobTitleDetailForEditOutput
        {
            CompetencyByJobTitleDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCompetencyByJobTitleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCompetencyByJobTitleDetailDto input)
    {
        var entity = MapToEntity(input, new CompetencyByJobTitleDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCompetencyByJobTitleDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CompetencyByJobTitleDetail MapToEntity(CreateOrEditCompetencyByJobTitleDetailDto input, CompetencyByJobTitleDetail entity)
    {
        entity.CompetencyByJobTitleId = input.CompetencyByJobTitleId;
        entity.CompetencyId = input.CompetencyId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate Competency per parent, and total weight for the
    /// parent should not exceed 100.
    /// </summary>
    private async Task ValidateAsync(CompetencyByJobTitleDetail entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.CompetencyByJobTitleId == entity.CompetencyByJobTitleId &&
                           d.CompetencyId == entity.CompetencyId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CompetencyAlreadyAssigned"));
        }

        var totalWeight = await _repository.GetAll()
            .Where(d => d.Id != entity.Id && d.CompetencyByJobTitleId == entity.CompetencyByJobTitleId)
            .SumAsync(d => (double?)d.Weight) ?? 0;

        if (totalWeight + entity.Weight > 100)
        {
            throw new UserFriendlyException(L("TotalWeightShouldNotExceed100"));
        }
    }

    private static CompetencyByJobTitleDetailDto ToDto(CompetencyByJobTitleDetail entity)
    {
        return new CompetencyByJobTitleDetailDto
        {
            Id = entity.Id,
            CompetencyByJobTitleId = entity.CompetencyByJobTitleId,
            CompetencyId = entity.CompetencyId,
            CompetencyName = entity.Competency?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }

    private static CreateOrEditCompetencyByJobTitleDetailDto ToCreateOrEditDto(CompetencyByJobTitleDetail entity)
    {
        return new CreateOrEditCompetencyByJobTitleDetailDto
        {
            Id = entity.Id,
            CompetencyByJobTitleId = entity.CompetencyByJobTitleId,
            CompetencyId = entity.CompetencyId,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }
}
