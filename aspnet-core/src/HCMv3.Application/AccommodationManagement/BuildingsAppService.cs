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
/// Application service for the Building master (ported from legacy XAF Building business object). Its
/// <c>Contract</c> aggregated child collection is exposed separately via
/// <see cref="BuildingContractsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Buildings)]
public class BuildingsAppService : HCMv3AppServiceBase, IBuildingsAppService
{
    private readonly IRepository<Building> _buildingRepository;

    public BuildingsAppService(IRepository<Building> buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BuildingDto>> GetAll(GetAllBuildingsInput input)
    {
        input.Normalize();

        var query = _buildingRepository.GetAll()
            .Include(b => b.Location)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), b =>
                b.Code.Contains(input.Filter) ||
                b.NameEn.Contains(input.Filter) ||
                b.NameAr.Contains(input.Filter))
            .WhereIf(input.LocationId.HasValue, b => b.LocationId == input.LocationId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BuildingDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBuildingForEditOutput> GetBuildingForEdit(EntityDto input)
    {
        var entity = await _buildingRepository.GetAsync(input.Id);

        return new GetBuildingForEditOutput
        {
            Building = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBuildingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Buildings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _buildingRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Buildings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBuildingDto input)
    {
        var entity = MapToEntity(input, new Building());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _buildingRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Buildings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBuildingDto input)
    {
        var entity = await _buildingRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _buildingRepository.UpdateAsync(entity);
    }

    private static Building MapToEntity(CreateOrEditBuildingDto input, Building entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.LocationId = input.LocationId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Building entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _buildingRepository.GetAll()
            .Where(b => b.Code != null && b.Code != "")
            .Select(b => b.Code)
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
    private async Task ValidateAsync(Building entity)
    {
        var duplicateCode = await _buildingRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id && b.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _buildingRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id &&
                           (b.NameEn == entity.NameEn || b.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static BuildingDto ToDto(Building entity)
    {
        return new BuildingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditBuildingDto ToCreateOrEditDto(Building entity)
    {
        return new CreateOrEditBuildingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            LocationId = entity.LocationId,
            Comments = entity.Comments
        };
    }
}
