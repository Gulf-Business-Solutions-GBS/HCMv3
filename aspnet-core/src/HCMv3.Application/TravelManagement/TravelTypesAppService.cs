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
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for the TravelType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelType</c>). Mirrors
/// <c>GeneralMasterInfo.LoanTypesAppService</c> -- Code/NameEn/NameAr shape plus a single <c>WageTypeId</c>
/// FK, so it can't derive from <see cref="Abp.Application.Services.ApplicationService"/>'s generic lookup
/// base. See <see cref="TravelType"/> class remarks for the dropped legacy
/// <c>TravelTypeWageType</c> UI-only filter collection.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelTypes)]
public class TravelTypesAppService : HCMv3AppServiceBase, ITravelTypesAppService
{
    private readonly IRepository<TravelType> _repository;

    public TravelTypesAppService(IRepository<TravelType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelTypeDto>> GetAll(GetAllTravelTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.WageType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelTypeForEditOutput> GetTravelTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelTypeForEditOutput
        {
            TravelType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelTypeDto input)
    {
        var entity = new TravelType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            WageTypeId = input.WageTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.WageTypeId = input.WageTypeId;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(TravelType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(TravelType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static TravelTypeDto ToDto(TravelType entity)
    {
        return new TravelTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn
        };
    }

    private static CreateOrEditTravelTypeDto ToCreateOrEditDto(TravelType entity)
    {
        return new CreateOrEditTravelTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WageTypeId = entity.WageTypeId
        };
    }
}
