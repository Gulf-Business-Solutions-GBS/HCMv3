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
/// Application service for the Bed master (ported from legacy XAF Bed business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Beds)]
public class BedsAppService : HCMv3AppServiceBase, IBedsAppService
{
    private readonly IRepository<Bed> _bedRepository;

    public BedsAppService(IRepository<Bed> bedRepository)
    {
        _bedRepository = bedRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BedDto>> GetAll(GetAllBedsInput input)
    {
        input.Normalize();

        var query = _bedRepository.GetAll()
            .Include(b => b.Room)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), b =>
                b.Code.Contains(input.Filter) ||
                b.NameEn.Contains(input.Filter) ||
                b.NameAr.Contains(input.Filter))
            .WhereIf(input.RoomId.HasValue, b => b.RoomId == input.RoomId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BedDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBedForEditOutput> GetBedForEdit(EntityDto input)
    {
        var entity = await _bedRepository.GetAsync(input.Id);

        return new GetBedForEditOutput
        {
            Bed = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBedDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Beds_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _bedRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Beds_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBedDto input)
    {
        var entity = MapToEntity(input, new Bed());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _bedRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Beds_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBedDto input)
    {
        var entity = await _bedRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _bedRepository.UpdateAsync(entity);
    }

    private static Bed MapToEntity(CreateOrEditBedDto input, Bed entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.RoomId = input.RoomId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Bed entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _bedRepository.GetAll()
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
    private async Task ValidateAsync(Bed entity)
    {
        var duplicateCode = await _bedRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id && b.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _bedRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id &&
                           (b.NameEn == entity.NameEn || b.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static BedDto ToDto(Bed entity)
    {
        return new BedDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            RoomId = entity.RoomId,
            RoomName = entity.Room?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditBedDto ToCreateOrEditDto(Bed entity)
    {
        return new CreateOrEditBedDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            RoomId = entity.RoomId,
            Comments = entity.Comments
        };
    }
}
