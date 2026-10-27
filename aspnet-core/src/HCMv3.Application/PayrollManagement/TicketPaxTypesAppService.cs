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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="TicketPaxType"/> (age-bucketed ticket passenger types). Ported from
/// legacy XAF TicketPaxType business object. Can't use <see cref="SimpleLookupAppServiceBase{TEntity}"/>
/// (bespoke shape: adds AgeFrom/AgeTo), so the shared Code/NameEn/NameAr validation is duplicated here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketPaxTypes)]
public class TicketPaxTypesAppService : HCMv3AppServiceBase, ITicketPaxTypesAppService
{
    private readonly IRepository<TicketPaxType> _repository;

    public TicketPaxTypesAppService(IRepository<TicketPaxType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TicketPaxTypeDto>> GetAll(GetAllTicketPaxTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TicketPaxTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTicketPaxTypeForEditOutput> GetTicketPaxTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTicketPaxTypeForEditOutput
        {
            TicketPaxType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTicketPaxTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTicketPaxTypeDto input)
    {
        var entity = MapToEntity(input, new TicketPaxType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTicketPaxTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static TicketPaxType MapToEntity(CreateOrEditTicketPaxTypeDto input, TicketPaxType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.AgeFrom = input.AgeFrom;
        entity.AgeTo = input.AgeTo;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(TicketPaxType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.</summary>
    private async Task ValidateUniquenessAsync(TicketPaxType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static TicketPaxTypeDto ToDto(TicketPaxType entity)
    {
        return new TicketPaxTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            AgeFrom = entity.AgeFrom,
            AgeTo = entity.AgeTo
        };
    }

    private static CreateOrEditTicketPaxTypeDto ToCreateOrEditDto(TicketPaxType entity)
    {
        return new CreateOrEditTicketPaxTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            AgeFrom = entity.AgeFrom,
            AgeTo = entity.AgeTo
        };
    }
}
