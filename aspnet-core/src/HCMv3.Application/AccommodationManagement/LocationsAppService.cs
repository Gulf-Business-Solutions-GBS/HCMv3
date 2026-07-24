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
/// Application service for the Location master (ported from legacy XAF Location business object). See
/// <see cref="Location"/> class remarks for why it does not derive from the shared
/// <c>SimpleLookupEntity</c>/<c>SimpleLookupAppServiceBase{TEntity}</c> infrastructure.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Locations)]
public class LocationsAppService : HCMv3AppServiceBase, ILocationsAppService
{
    private readonly IRepository<Location> _locationRepository;

    public LocationsAppService(IRepository<Location> locationRepository)
    {
        _locationRepository = locationRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<LocationDto>> GetAll(GetAllLocationsInput input)
    {
        input.Normalize();

        var query = _locationRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), l =>
                l.Code.Contains(input.Filter) ||
                l.NameEn.Contains(input.Filter) ||
                l.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LocationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetLocationForEditOutput> GetLocationForEdit(EntityDto input)
    {
        var entity = await _locationRepository.GetAsync(input.Id);

        return new GetLocationForEditOutput
        {
            Location = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditLocationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Locations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _locationRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Locations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLocationDto input)
    {
        var entity = MapToEntity(input, new Location());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _locationRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Locations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLocationDto input)
    {
        var entity = await _locationRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _locationRepository.UpdateAsync(entity);
    }

    private static Location MapToEntity(CreateOrEditLocationDto input, Location entity)
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
    private async Task AssignCodeIfEmptyAsync(Location entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _locationRepository.GetAll()
            .Where(l => l.Code != null && l.Code != "")
            .Select(l => l.Code)
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
    private async Task ValidateAsync(Location entity)
    {
        var duplicateCode = await _locationRepository.GetAll()
            .AnyAsync(l => l.Id != entity.Id && l.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _locationRepository.GetAll()
            .AnyAsync(l => l.Id != entity.Id &&
                           (l.NameEn == entity.NameEn || l.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static LocationDto ToDto(Location entity)
    {
        return new LocationDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditLocationDto ToCreateOrEditDto(Location entity)
    {
        return new CreateOrEditLocationDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Comments = entity.Comments
        };
    }
}
