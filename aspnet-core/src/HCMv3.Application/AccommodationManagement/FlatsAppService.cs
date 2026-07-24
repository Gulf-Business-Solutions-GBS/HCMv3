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
/// Application service for the Flat master (ported from legacy XAF Flat business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Flats)]
public class FlatsAppService : HCMv3AppServiceBase, IFlatsAppService
{
    private readonly IRepository<Flat> _flatRepository;

    public FlatsAppService(IRepository<Flat> flatRepository)
    {
        _flatRepository = flatRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<FlatDto>> GetAll(GetAllFlatsInput input)
    {
        input.Normalize();

        var query = _flatRepository.GetAll()
            .Include(f => f.Floor)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), f =>
                f.Code.Contains(input.Filter) ||
                f.NameEn.Contains(input.Filter) ||
                f.NameAr.Contains(input.Filter))
            .WhereIf(input.FloorId.HasValue, f => f.FloorId == input.FloorId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<FlatDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetFlatForEditOutput> GetFlatForEdit(EntityDto input)
    {
        var entity = await _flatRepository.GetAsync(input.Id);

        return new GetFlatForEditOutput
        {
            Flat = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditFlatDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Flats_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _flatRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Flats_Create)]
    protected virtual async Task CreateAsync(CreateOrEditFlatDto input)
    {
        var entity = MapToEntity(input, new Flat());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _flatRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_Flats_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditFlatDto input)
    {
        var entity = await _flatRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _flatRepository.UpdateAsync(entity);
    }

    private static Flat MapToEntity(CreateOrEditFlatDto input, Flat entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.FloorId = input.FloorId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Flat entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _flatRepository.GetAll()
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
    private async Task ValidateAsync(Flat entity)
    {
        var duplicateCode = await _flatRepository.GetAll()
            .AnyAsync(f => f.Id != entity.Id && f.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _flatRepository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           (f.NameEn == entity.NameEn || f.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static FlatDto ToDto(Flat entity)
    {
        return new FlatDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            FloorId = entity.FloorId,
            FloorName = entity.Floor?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditFlatDto ToCreateOrEditDto(Flat entity)
    {
        return new CreateOrEditFlatDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            FloorId = entity.FloorId,
            Comments = entity.Comments
        };
    }
}
