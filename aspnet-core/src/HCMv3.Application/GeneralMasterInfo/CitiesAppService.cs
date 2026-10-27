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
/// Application service for the City master (ported from legacy XAF City business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly) since
/// the list DTO also needs the FK's display name (StateName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Cities)]
public class CitiesAppService : HCMv3AppServiceBase, ICitiesAppService
{
    private readonly IRepository<City> _cityRepository;

    public CitiesAppService(IRepository<City> cityRepository)
    {
        _cityRepository = cityRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CityDto>> GetAll(GetAllCitiesInput input)
    {
        input.Normalize();

        var query = _cityRepository.GetAll()
            .Include(c => c.State)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CityDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCityForEditOutput> GetCityForEdit(EntityDto input)
    {
        var city = await _cityRepository.GetAsync(input.Id);

        return new GetCityForEditOutput
        {
            City = ToCreateOrEditDto(city)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Cities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCityDto input)
    {
        var city = new City
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            StateId = input.StateId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(city);
        await ValidateUniquenessAsync(city);

        await _cityRepository.InsertAsync(city);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Cities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCityDto input)
    {
        var city = await _cityRepository.GetAsync(input.Id.Value);

        city.Code = input.Code;
        city.NameEn = input.NameEn;
        city.NameAr = input.NameAr;
        city.StateId = input.StateId;

        await AssignCodeIfEmptyAsync(city);
        await ValidateUniquenessAsync(city);

        await _cityRepository.UpdateAsync(city);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Cities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _cityRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(City city)
    {
        if (!city.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _cityRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        city.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(City city)
    {
        var duplicateCode = await _cityRepository.GetAll()
            .AnyAsync(c => c.Id != city.Id && c.Code == city.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _cityRepository.GetAll()
            .AnyAsync(c => c.Id != city.Id &&
                           (c.NameEn == city.NameEn || c.NameAr == city.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CityDto ToDto(City city)
    {
        return new CityDto
        {
            Id = city.Id,
            Code = city.Code,
            NameEn = city.NameEn,
            NameAr = city.NameAr,
            StateId = city.StateId,
            StateName = city.State?.NameEn
        };
    }

    private static CreateOrEditCityDto ToCreateOrEditDto(City city)
    {
        return new CreateOrEditCityDto
        {
            Id = city.Id,
            Code = city.Code,
            NameEn = city.NameEn,
            NameAr = city.NameAr,
            StateId = city.StateId
        };
    }
}
