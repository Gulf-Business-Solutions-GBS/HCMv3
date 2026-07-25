using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SuccessionManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Application service for <see cref="NineBoxMapping"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.NineBoxMapping</c>). Only the legacy required-field
/// validation is reproduced -- the legacy business object had no uniqueness check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_NineBoxMappings)]
public class NineBoxMappingsAppService : HCMv3AppServiceBase, INineBoxMappingsAppService
{
    private readonly IRepository<NineBoxMapping> _repository;

    public NineBoxMappingsAppService(IRepository<NineBoxMapping> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NineBoxMappingDto>> GetAll(GetAllNineBoxMappingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Rating)
            .Include(e => e.NineBoxRating)
            .WhereIf(input.RatingId.HasValue, e => e.RatingId == input.RatingId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NineBoxMappingDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetNineBoxMappingForEditOutput> GetNineBoxMappingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNineBoxMappingForEditOutput
        {
            NineBoxMapping = new CreateOrEditNineBoxMappingDto
            {
                Id = entity.Id,
                RatingId = entity.RatingId,
                NineBoxRatingId = entity.NineBoxRatingId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditNineBoxMappingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNineBoxMappingDto input)
    {
        var entity = MapToEntity(input, new NineBoxMapping());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNineBoxMappingDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static NineBoxMapping MapToEntity(CreateOrEditNineBoxMappingDto input, NineBoxMapping entity)
    {
        entity.RatingId = input.RatingId;
        entity.NineBoxRatingId = input.NineBoxRatingId;
        return entity;
    }

    private static NineBoxMappingDto ToDto(NineBoxMapping entity)
    {
        return new NineBoxMappingDto
        {
            Id = entity.Id,
            RatingId = entity.RatingId,
            RatingNameEn = entity.Rating?.NameEn,
            NineBoxRatingId = entity.NineBoxRatingId,
            NineBoxRatingNameEn = entity.NineBoxRating?.NameEn
        };
    }
}
