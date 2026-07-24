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
using HCMv3.AccommodationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Application service for the AccommodationType master (ported from legacy XAF AccommodationType business
/// object). See <see cref="AccommodationType"/> class remarks for why it does not derive from the shared
/// <c>SimpleLookupEntity</c>/<c>SimpleLookupAppServiceBase{TEntity}</c> infrastructure.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationTypes)]
public class AccommodationTypesAppService : HCMv3AppServiceBase, IAccommodationTypesAppService
{
    private readonly IRepository<AccommodationType> _accommodationTypeRepository;

    public AccommodationTypesAppService(IRepository<AccommodationType> accommodationTypeRepository)
    {
        _accommodationTypeRepository = accommodationTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AccommodationTypeDto>> GetAll(GetAllAccommodationTypesInput input)
    {
        input.Normalize();

        var query = _accommodationTypeRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AccommodationTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAccommodationTypeForEditOutput> GetAccommodationTypeForEdit(EntityDto input)
    {
        var entity = await _accommodationTypeRepository.GetAsync(input.Id);

        return new GetAccommodationTypeForEditOutput
        {
            AccommodationType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAccommodationTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _accommodationTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAccommodationTypeDto input)
    {
        var entity = MapToEntity(input, new AccommodationType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accommodationTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAccommodationTypeDto input)
    {
        var entity = await _accommodationTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accommodationTypeRepository.UpdateAsync(entity);
    }

    private static AccommodationType MapToEntity(CreateOrEditAccommodationTypeDto input, AccommodationType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AccommodationType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _accommodationTypeRepository.GetAll()
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
    /// Legacy <c>PerformValidations</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateAsync(AccommodationType entity)
    {
        var duplicateCode = await _accommodationTypeRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _accommodationTypeRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static AccommodationTypeDto ToDto(AccommodationType entity)
    {
        return new AccommodationTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditAccommodationTypeDto ToCreateOrEditDto(AccommodationType entity)
    {
        return new CreateOrEditAccommodationTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }
}
