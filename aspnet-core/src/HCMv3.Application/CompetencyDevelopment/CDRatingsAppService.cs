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
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for the annual-year-review rating lookup (ported from legacy XAF CDRating business
/// object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDRatings)]
public class CDRatingsAppService : HCMv3AppServiceBase, ICDRatingsAppService
{
    private readonly IRepository<CDRating> _repository;

    public CDRatingsAppService(IRepository<CDRating> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDRatingDto>> GetAll(GetAllCDRatingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDRatingDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDRatingForEditOutput> GetCDRatingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDRatingForEditOutput
        {
            CDRating = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDRatingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDRatingDto input)
    {
        var entity = MapToEntity(input, new CDRating());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDRatingDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDRating MapToEntity(CreateOrEditCDRatingDto input, CDRating entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(CDRating entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.</summary>
    private async Task ValidateAsync(CDRating entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CDRatingDto ToDto(CDRating entity)
    {
        return new CDRatingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditCDRatingDto ToCreateOrEditDto(CDRating entity)
    {
        return new CreateOrEditCDRatingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
