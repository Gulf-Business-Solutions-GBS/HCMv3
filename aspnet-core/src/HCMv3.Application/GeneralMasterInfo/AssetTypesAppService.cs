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
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the AssetType master (ported from legacy XAF AssetType). Was a simple lookup;
/// now has its own DTOs for the legacy <c>IncludeInWorkflow</c> and <c>WageType</c> fields. Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is kept exactly as the shared
/// simple-lookup base applied it. Same bespoke pattern as <see cref="DisciplinaryTypesAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AssetTypes)]
public class AssetTypesAppService : HCMv3AppServiceBase, IAssetTypesAppService
{
    private readonly IRepository<AssetType> _repository;

    public AssetTypesAppService(IRepository<AssetType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AssetTypeDto>> GetAll(GetAllAssetTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.WageType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AssetTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAssetTypeForEditOutput> GetAssetTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetAssetTypeForEditOutput
        {
            AssetType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAssetTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAssetTypeDto input)
    {
        var entity = MapToEntity(input, new AssetType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAssetTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AssetType entity)
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
    private async Task ValidateUniquenessAsync(AssetType entity)
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

    private static AssetType MapToEntity(CreateOrEditAssetTypeDto input, AssetType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IncludeInWorkflow = input.IncludeInWorkflow;
        entity.WageTypeId = input.WageTypeId;
        return entity;
    }

    private static AssetTypeDto ToDto(AssetType entity)
    {
        return new AssetTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn
        };
    }

    private static CreateOrEditAssetTypeDto ToCreateOrEditDto(AssetType entity)
    {
        return new CreateOrEditAssetTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            WageTypeId = entity.WageTypeId
        };
    }
}
