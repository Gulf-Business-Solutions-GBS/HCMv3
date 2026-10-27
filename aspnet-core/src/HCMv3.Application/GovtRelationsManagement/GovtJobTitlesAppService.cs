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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="GovtJobTitle"/> (ported from legacy XAF GovtJobTitle business
/// object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles)]
public class GovtJobTitlesAppService : HCMv3AppServiceBase, IGovtJobTitlesAppService
{
    private readonly IRepository<GovtJobTitle> _repository;

    public GovtJobTitlesAppService(IRepository<GovtJobTitle> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GovtJobTitleDto>> GetAll(GetAllGovtJobTitlesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.GovtJobCategory)
            .WhereIf(input.GovtJobCategoryId.HasValue, t => t.GovtJobCategoryId == input.GovtJobCategoryId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GovtJobTitleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetGovtJobTitleForEditOutput> GetGovtJobTitleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGovtJobTitleForEditOutput
        {
            GovtJobTitle = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGovtJobTitleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGovtJobTitleDto input)
    {
        var entity = MapToEntity(input, new GovtJobTitle());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGovtJobTitleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static GovtJobTitle MapToEntity(CreateOrEditGovtJobTitleDto input, GovtJobTitle entity)
    {
        entity.GovtJobCategoryId = input.GovtJobCategoryId;
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(GovtJobTitle entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateAsync(GovtJobTitle entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static GovtJobTitleDto ToDto(GovtJobTitle entity)
    {
        return new GovtJobTitleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            GovtJobCategoryId = entity.GovtJobCategoryId,
            GovtJobCategoryName = entity.GovtJobCategory?.NameEn
        };
    }

    private static CreateOrEditGovtJobTitleDto ToCreateOrEditDto(GovtJobTitle entity)
    {
        return new CreateOrEditGovtJobTitleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            GovtJobCategoryId = entity.GovtJobCategoryId
        };
    }
}
