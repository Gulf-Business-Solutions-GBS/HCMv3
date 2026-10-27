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
using HCMv3.GatePassManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GatePassManagement;

/// <summary>
/// Application service for the GatePassVendor master (ported from legacy XAF GatePassVendor business
/// object). See <see cref="GatePassVendor"/> class remarks for why it does not derive from the shared
/// <c>SimpleLookupEntity</c>/<c>SimpleLookupAppServiceBase{TEntity}</c> infrastructure.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVendors)]
public class GatePassVendorsAppService : HCMv3AppServiceBase, IGatePassVendorsAppService
{
    private readonly IRepository<GatePassVendor> _repository;

    public GatePassVendorsAppService(IRepository<GatePassVendor> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GatePassVendorDto>> GetAll(GetAllGatePassVendorsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), v =>
                v.Code.Contains(input.Filter) ||
                v.NameEn.Contains(input.Filter) ||
                v.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GatePassVendorDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGatePassVendorForEditOutput> GetGatePassVendorForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetGatePassVendorForEditOutput
        {
            GatePassVendor = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGatePassVendorDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVendors_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVendors_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGatePassVendorDto input)
    {
        var entity = MapToEntity(input, new GatePassVendor());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassVendors_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGatePassVendorDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static GatePassVendor MapToEntity(CreateOrEditGatePassVendorDto input, GatePassVendor entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.VendorAddress = input.VendorAddress;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(GatePassVendor entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(v => v.Code != null && v.Code != "")
            .Select(v => v.Code)
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
    private async Task ValidateAsync(GatePassVendor entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(v => v.Id != entity.Id && v.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(v => v.Id != entity.Id &&
                           (v.NameEn == entity.NameEn || v.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static GatePassVendorDto ToDto(GatePassVendor entity)
    {
        return new GatePassVendorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            VendorAddress = entity.VendorAddress
        };
    }

    private static CreateOrEditGatePassVendorDto ToCreateOrEditDto(GatePassVendor entity)
    {
        return new CreateOrEditGatePassVendorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            VendorAddress = entity.VendorAddress
        };
    }
}
