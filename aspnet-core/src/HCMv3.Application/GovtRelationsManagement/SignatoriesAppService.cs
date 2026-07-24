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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="Signatory"/> (ported from legacy XAF Signatory business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Signatories)]
public class SignatoriesAppService : HCMv3AppServiceBase, ISignatoriesAppService
{
    private readonly IRepository<Signatory> _repository;

    public SignatoriesAppService(IRepository<Signatory> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SignatoryDto>> GetAll(GetAllSignatoriesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Code.Contains(input.Filter) ||
                s.NameEn.Contains(input.Filter) ||
                s.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SignatoryDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSignatoryForEditOutput> GetSignatoryForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSignatoryForEditOutput
        {
            Signatory = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSignatoryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Signatories_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Signatories_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSignatoryDto input)
    {
        var entity = MapToEntity(input, new Signatory());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Signatories_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSignatoryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static Signatory MapToEntity(CreateOrEditSignatoryDto input, Signatory entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.CivilId = input.CivilId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Signatory entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(s => s.Code != null && s.Code != "")
            .Select(s => s.Code)
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
    private async Task ValidateAsync(Signatory entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id && s.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id &&
                           (s.NameEn == entity.NameEn || s.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SignatoryDto ToDto(Signatory entity)
    {
        return new SignatoryDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            CivilId = entity.CivilId
        };
    }

    private static CreateOrEditSignatoryDto ToCreateOrEditDto(Signatory entity)
    {
        return new CreateOrEditSignatoryDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            CivilId = entity.CivilId
        };
    }
}
