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
/// Application service for the AccommodationServiceType master (ported from legacy XAF
/// AccommodationServiceType business object). See <see cref="AccommodationServiceType"/> class remarks for
/// why it does not derive from the shared
/// <c>SimpleLookupEntity</c>/<c>SimpleLookupAppServiceBase{TEntity}</c> infrastructure.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes)]
public class AccommodationServiceTypesAppService : HCMv3AppServiceBase, IAccommodationServiceTypesAppService
{
    private readonly IRepository<AccommodationServiceType> _accommodationServiceTypeRepository;

    public AccommodationServiceTypesAppService(IRepository<AccommodationServiceType> accommodationServiceTypeRepository)
    {
        _accommodationServiceTypeRepository = accommodationServiceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AccommodationServiceTypeDto>> GetAll(GetAllAccommodationServiceTypesInput input)
    {
        input.Normalize();

        var query = _accommodationServiceTypeRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AccommodationServiceTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAccommodationServiceTypeForEditOutput> GetAccommodationServiceTypeForEdit(EntityDto input)
    {
        var entity = await _accommodationServiceTypeRepository.GetAsync(input.Id);

        return new GetAccommodationServiceTypeForEditOutput
        {
            AccommodationServiceType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAccommodationServiceTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _accommodationServiceTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAccommodationServiceTypeDto input)
    {
        var entity = MapToEntity(input, new AccommodationServiceType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accommodationServiceTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAccommodationServiceTypeDto input)
    {
        var entity = await _accommodationServiceTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accommodationServiceTypeRepository.UpdateAsync(entity);
    }

    private static AccommodationServiceType MapToEntity(CreateOrEditAccommodationServiceTypeDto input, AccommodationServiceType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr (Code auto-assign when
    /// blank, same as <c>AccommodationType</c>).
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AccommodationServiceType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _accommodationServiceTypeRepository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateAsync(AccommodationServiceType entity)
    {
        var duplicateCode = await _accommodationServiceTypeRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _accommodationServiceTypeRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static AccommodationServiceTypeDto ToDto(AccommodationServiceType entity)
    {
        return new AccommodationServiceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditAccommodationServiceTypeDto ToCreateOrEditDto(AccommodationServiceType entity)
    {
        return new CreateOrEditAccommodationServiceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }
}
