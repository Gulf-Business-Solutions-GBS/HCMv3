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
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for the report-builder domain/entity-type lookup (ported from legacy XAF DomainType
/// business object). See <see cref="DomainType"/> class remarks for why it is not a
/// <c>SimpleLookupEntity</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_DomainTypes)]
public class DomainTypesAppService : HCMv3AppServiceBase, IDomainTypesAppService
{
    private readonly IRepository<DomainType> _repository;

    public DomainTypesAppService(IRepository<DomainType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DomainTypeDto>> GetAll(GetAllDomainTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DomainTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetDomainTypeForEditOutput> GetDomainTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetDomainTypeForEditOutput
        {
            DomainType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDomainTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_DomainTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_DomainTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDomainTypeDto input)
    {
        var entity = MapToEntity(input, new DomainType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_DomainTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDomainTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static DomainType MapToEntity(CreateOrEditDomainTypeDto input, DomainType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.EntityType = input.EntityType;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(DomainType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(d => d.Code != null && d.Code != "")
            .Select(d => d.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn.</summary>
    private async Task ValidateAsync(DomainType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.NameEn == entity.NameEn);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static DomainTypeDto ToDto(DomainType entity)
    {
        return new DomainTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            EntityType = entity.EntityType
        };
    }

    private static CreateOrEditDomainTypeDto ToCreateOrEditDto(DomainType entity)
    {
        return new CreateOrEditDomainTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            EntityType = entity.EntityType
        };
    }
}
