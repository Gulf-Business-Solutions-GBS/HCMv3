using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the cost-distribution line of a <see cref="CostRegister"/> (ported from legacy
/// XAF CostRegisterDetail business object), listed separately and linked back via
/// <see cref="CostRegisterDetail.CostRegisterId"/>. No business validation in the legacy source (plain
/// CRUD). See <see cref="CostRegisterDetail"/> remarks for the currency-default simplification note.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisterDetails)]
public class CostRegisterDetailsAppService : HCMv3AppServiceBase, ICostRegisterDetailsAppService
{
    private readonly IRepository<CostRegisterDetail> _costRegisterDetailRepository;

    public CostRegisterDetailsAppService(IRepository<CostRegisterDetail> costRegisterDetailRepository)
    {
        _costRegisterDetailRepository = costRegisterDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CostRegisterDetailDto>> GetAll(GetAllCostRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _costRegisterDetailRepository.GetAll()
            .Include(d => d.WageType)
            .Include(d => d.AccountInfo)
            .WhereIf(input.CostRegisterId.HasValue, d => d.CostRegisterId == input.CostRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CostRegisterDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCostRegisterDetailForEditOutput> GetCostRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _costRegisterDetailRepository.GetAsync(input.Id);

        return new GetCostRegisterDetailForEditOutput
        {
            CostRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCostRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _costRegisterDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCostRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new CostRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _costRegisterDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCostRegisterDetailDto input)
    {
        var entity = await _costRegisterDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _costRegisterDetailRepository.UpdateAsync(entity);
    }

    private static CostRegisterDetail MapToEntity(CreateOrEditCostRegisterDetailDto input, CostRegisterDetail entity)
    {
        entity.CostRegisterId = input.CostRegisterId;
        entity.WageTypeId = input.WageTypeId;
        entity.AccountInfoId = input.AccountInfoId;
        entity.CostElement1Id = input.CostElement1Id;
        entity.CostElement2Id = input.CostElement2Id;
        entity.CostElement3Id = input.CostElement3Id;
        entity.CostElement4Id = input.CostElement4Id;
        entity.CostElement5Id = input.CostElement5Id;
        entity.CostElement6Id = input.CostElement6Id;
        entity.CostElement7Id = input.CostElement7Id;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;

        return entity;
    }

    private static CostRegisterDetailDto ToDto(CostRegisterDetail entity)
    {
        return new CostRegisterDetailDto
        {
            Id = entity.Id,
            CostRegisterId = entity.CostRegisterId,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            AccountInfoId = entity.AccountInfoId,
            AccountInfoName = entity.AccountInfo?.NameEn,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId
        };
    }

    private static CreateOrEditCostRegisterDetailDto ToCreateOrEditDto(CostRegisterDetail entity)
    {
        return new CreateOrEditCostRegisterDetailDto
        {
            Id = entity.Id,
            CostRegisterId = entity.CostRegisterId,
            WageTypeId = entity.WageTypeId,
            AccountInfoId = entity.AccountInfoId,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId
        };
    }
}
