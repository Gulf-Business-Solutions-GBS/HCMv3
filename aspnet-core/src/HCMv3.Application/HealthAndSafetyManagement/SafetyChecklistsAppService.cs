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
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the safety checklist master, scoped to a <see cref="SafetyAreaAsset"/>
/// (ported from legacy XAF SafetyChecklist business object). Legacy <c>PerformValidation</c> required
/// Code (auto-assigned if blank, unique) and SafetyAreaAsset -- ported as-is here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists)]
public class SafetyChecklistsAppService : HCMv3AppServiceBase, ISafetyChecklistsAppService
{
    private readonly IRepository<SafetyChecklist> _safetyChecklistRepository;

    public SafetyChecklistsAppService(IRepository<SafetyChecklist> safetyChecklistRepository)
    {
        _safetyChecklistRepository = safetyChecklistRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SafetyChecklistDto>> GetAll(GetAllSafetyChecklistsInput input)
    {
        input.Normalize();

        var query = _safetyChecklistRepository.GetAll()
            .Include(c => c.SafetyAreaAsset)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c => c.Code.Contains(input.Filter))
            .WhereIf(input.SafetyAreaAssetId.HasValue, c => c.SafetyAreaAssetId == input.SafetyAreaAssetId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SafetyChecklistDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSafetyChecklistForEditOutput> GetSafetyChecklistForEdit(EntityDto input)
    {
        var entity = await _safetyChecklistRepository.GetAsync(input.Id);

        return new GetSafetyChecklistForEditOutput
        {
            SafetyChecklist = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSafetyChecklistDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _safetyChecklistRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSafetyChecklistDto input)
    {
        var entity = MapToEntity(input, new SafetyChecklist());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _safetyChecklistRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSafetyChecklistDto input)
    {
        var entity = await _safetyChecklistRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _safetyChecklistRepository.UpdateAsync(entity);
    }

    private static SafetyChecklist MapToEntity(CreateOrEditSafetyChecklistDto input, SafetyChecklist entity)
    {
        entity.Code = input.Code;
        entity.SafetyAreaAssetId = input.SafetyAreaAssetId;
        entity.Description = input.Description;
        entity.IsActive = input.IsActive;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(SafetyChecklist entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _safetyChecklistRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code.
    /// </summary>
    private async Task ValidateUniquenessAsync(SafetyChecklist entity)
    {
        var duplicateCode = await _safetyChecklistRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id && c.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }
    }

    private static SafetyChecklistDto ToDto(SafetyChecklist entity)
    {
        return new SafetyChecklistDto
        {
            Id = entity.Id,
            Code = entity.Code,
            SafetyAreaAssetId = entity.SafetyAreaAssetId,
            SafetyAreaAssetNameEn = entity.SafetyAreaAsset?.NameEn,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditSafetyChecklistDto ToCreateOrEditDto(SafetyChecklist entity)
    {
        return new CreateOrEditSafetyChecklistDto
        {
            Id = entity.Id,
            Code = entity.Code,
            SafetyAreaAssetId = entity.SafetyAreaAssetId,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }
}
