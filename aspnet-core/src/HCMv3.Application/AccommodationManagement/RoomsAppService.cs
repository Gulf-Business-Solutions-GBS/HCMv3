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
/// Application service for the Room master (ported from legacy XAF Room business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Rooms)]
public class RoomsAppService : HCMv3AppServiceBase, IRoomsAppService
{
    private readonly IRepository<Room> _roomRepository;

    public RoomsAppService(IRepository<Room> roomRepository)
    {
        _roomRepository = roomRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RoomDto>> GetAll(GetAllRoomsInput input)
    {
        input.Normalize();

        var query = _roomRepository.GetAll()
            .Include(r => r.Flat)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter))
            .WhereIf(input.FlatId.HasValue, r => r.FlatId == input.FlatId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RoomDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRoomForEditOutput> GetRoomForEdit(EntityDto input)
    {
        var entity = await _roomRepository.GetAsync(input.Id);

        return new GetRoomForEditOutput
        {
            Room = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRoomDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Rooms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _roomRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Rooms_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRoomDto input)
    {
        var entity = MapToEntity(input, new Room());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _roomRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Rooms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRoomDto input)
    {
        var entity = await _roomRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _roomRepository.UpdateAsync(entity);
    }

    private static Room MapToEntity(CreateOrEditRoomDto input, Room entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.FlatId = input.FlatId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Room entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _roomRepository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
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
    private async Task ValidateAsync(Room entity)
    {
        var duplicateCode = await _roomRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _roomRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static RoomDto ToDto(Room entity)
    {
        return new RoomDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            FlatId = entity.FlatId,
            FlatName = entity.Flat?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditRoomDto ToCreateOrEditDto(Room entity)
    {
        return new CreateOrEditRoomDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            FlatId = entity.FlatId,
            Comments = entity.Comments
        };
    }
}
