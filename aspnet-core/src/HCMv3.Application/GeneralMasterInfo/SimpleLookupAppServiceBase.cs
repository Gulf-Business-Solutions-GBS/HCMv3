using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Generic application service for the pure bilingual lookup entities ported from the legacy XAF
/// GeneralMasterInfo module (Code/NameEn/NameAr only). Concrete subclasses (e.g. CountriesAppService)
/// just supply the entity type, permission names, and the repository via DI — see AccessUnitsAppService
/// for the pattern this generalizes.
///
/// Legacy XAF <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle, shared by every lookup that derives from this base.
/// </summary>
public abstract class SimpleLookupAppServiceBase<TEntity> : HCMv3AppServiceBase, ISimpleLookupAppService
    where TEntity : SimpleLookupEntity, new()
{
    protected readonly IRepository<TEntity> Repository;

    protected abstract string CreatePermissionName { get; }
    protected abstract string EditPermissionName { get; }
    protected abstract string DeletePermissionName { get; }

    protected SimpleLookupAppServiceBase(IRepository<TEntity> repository)
    {
        Repository = repository;
    }

    [HttpPost]
    public virtual async Task<PagedResultDto<SimpleLookupDto>> GetAll(GetAllSimpleLookupsInput input)
    {
        input.Normalize();

        var query = Repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) ||
                e.NameEn.Contains(input.Filter) ||
                e.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimpleLookupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public virtual async Task<CreateOrEditSimpleLookupDto> GetForEdit(EntityDto input)
    {
        var entity = await Repository.GetAsync(input.Id);
        return ToCreateOrEditDto(entity);
    }

    public virtual async Task CreateOrEdit(CreateOrEditSimpleLookupDto input)
    {
        if (input.Id.HasValue)
        {
            await PermissionChecker.AuthorizeAsync(EditPermissionName);
            await UpdateAsync(input);
        }
        else
        {
            await PermissionChecker.AuthorizeAsync(CreatePermissionName);
            await CreateAsync(input);
        }
    }

    public virtual async Task Delete(EntityDto input)
    {
        await PermissionChecker.AuthorizeAsync(DeletePermissionName);
        await Repository.DeleteAsync(input.Id);
    }

    protected virtual async Task CreateAsync(CreateOrEditSimpleLookupDto input)
    {
        var entity = new TEntity
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await Repository.InsertAsync(entity);
    }

    protected virtual async Task UpdateAsync(CreateOrEditSimpleLookupDto input)
    {
        var entity = await Repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await Repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(TEntity entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await Repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
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
    private async Task ValidateUniquenessAsync(TEntity entity)
    {
        var duplicateCode = await Repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await Repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SimpleLookupDto ToDto(TEntity entity)
    {
        return new SimpleLookupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditSimpleLookupDto ToCreateOrEditDto(TEntity entity)
    {
        return new CreateOrEditSimpleLookupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
