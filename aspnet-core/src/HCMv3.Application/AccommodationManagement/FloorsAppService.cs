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
/// Application service for the Floor master (ported from legacy XAF Floor business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Floors)]
public class FloorsAppService : HCMv3AppServiceBase, IFloorsAppService
{
    private readonly IRepository<Floor> _floorRepository;

    public FloorsAppService(IRepository<Floor> floorRepository)
    {
        _floorRepository = floorRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<FloorDto>> GetAll(GetAllFloorsInput input)
    {
        input.Normalize();

        var query = _floorRepository.GetAll()
            .Include(f => f.Building)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), f =>
                f.Code.Contains(input.Filter) ||
                f.NameEn.Contains(input.Filter) ||
                f.NameAr.Contains(input.Filter))
            .WhereIf(input.BuildingId.HasValue, f => f.BuildingId == input.BuildingId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<FloorDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetFloorForEditOutput> GetFloorForEdit(EntityDto input)
    {
        var entity = await _floorRepository.GetAsync(input.Id);

        return new GetFloorForEditOutput
        {
            Floor = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditFloorDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Floors_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _floorRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Floors_Create)]
    protected virtual async Task CreateAsync(CreateOrEditFloorDto input)
    {
        var entity = MapToEntity(input, new Floor());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _floorRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Floors_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditFloorDto input)
    {
        var entity = await _floorRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _floorRepository.UpdateAsync(entity);
    }

    private static Floor MapToEntity(CreateOrEditFloorDto input, Floor entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.BuildingId = input.BuildingId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Floor entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _floorRepository.GetAll()
            .Where(f => f.Code != null && f.Code != "")
            .Select(f => f.Code)
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
    private async Task ValidateAsync(Floor entity)
    {
        var duplicateCode = await _floorRepository.GetAll()
            .AnyAsync(f => f.Id != entity.Id && f.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _floorRepository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           (f.NameEn == entity.NameEn || f.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static FloorDto ToDto(Floor entity)
    {
        return new FloorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            BuildingId = entity.BuildingId,
            BuildingName = entity.Building?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditFloorDto ToCreateOrEditDto(Floor entity)
    {
        return new CreateOrEditFloorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            BuildingId = entity.BuildingId,
            Comments = entity.Comments
        };
    }
}
