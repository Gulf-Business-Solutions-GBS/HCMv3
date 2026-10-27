using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the effective-dated FI account mapping of a <see cref="WageType"/> (ported from
/// legacy XAF WageAccount business object). See <see cref="WageAccount"/> class remarks for the dropped
/// CostElement-slot CostElementType cross-check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccounts)]
public class WageAccountsAppService : HCMv3AppServiceBase, IWageAccountsAppService
{
    private readonly IRepository<WageAccount> _wageAccountRepository;

    public WageAccountsAppService(IRepository<WageAccount> wageAccountRepository)
    {
        _wageAccountRepository = wageAccountRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageAccountDto>> GetAll(GetAllWageAccountsInput input)
    {
        input.Normalize();

        var query = _wageAccountRepository.GetAll()
            .Include(a => a.WageType)
            .Include(a => a.AccountInfo)
            .Include(a => a.OffsetAccountInfo)
            .WhereIf(input.WageTypeId.HasValue, a => a.WageTypeId == input.WageTypeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageAccountDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageAccountForEditOutput> GetWageAccountForEdit(EntityDto input)
    {
        var entity = await _wageAccountRepository.GetAsync(input.Id);

        return new GetWageAccountForEditOutput
        {
            WageAccount = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageAccountDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccounts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _wageAccountRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccounts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageAccountDto input)
    {
        var entity = MapToEntity(input, new WageAccount());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _wageAccountRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccounts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageAccountDto input)
    {
        var entity = await _wageAccountRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageAccountRepository.UpdateAsync(entity);
    }

    private static WageAccount MapToEntity(CreateOrEditWageAccountDto input, WageAccount entity)
    {
        entity.WageTypeId = input.WageTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.AccountInfoId = input.AccountInfoId;
        entity.OffsetAccountInfoId = input.OffsetAccountInfoId;
        entity.CostElement1Id = input.CostElement1Id;
        entity.CostElement2Id = input.CostElement2Id;
        entity.CostElement3Id = input.CostElement3Id;
        entity.CostElement4Id = input.CostElement4Id;
        entity.CostElement5Id = input.CostElement5Id;
        entity.CostElement6Id = input.CostElement6Id;
        entity.CostElement7Id = input.CostElement7Id;
        entity.CostPercentage = input.CostPercentage;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity: required fields, EndDate &gt;= StartDate, no overlapping
    /// date range with a sibling WageAccount for the same WageType (relying on ABP's standard soft-delete
    /// query filter, same note as <c>EmployeeSubGroupFundsAppService</c>).
    /// </summary>
    private async Task ValidateAsync(WageAccount entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _wageAccountRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.WageTypeId == entity.WageTypeId &&
                           a.StartDate.Date <= entity.EndDate.Date &&
                           a.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static WageAccountDto ToDto(WageAccount entity)
    {
        return new WageAccountDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AccountInfoId = entity.AccountInfoId,
            AccountInfoName = entity.AccountInfo?.NameEn,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            OffsetAccountInfoName = entity.OffsetAccountInfo?.NameEn,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage
        };
    }

    private static CreateOrEditWageAccountDto ToCreateOrEditDto(WageAccount entity)
    {
        return new CreateOrEditWageAccountDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AccountInfoId = entity.AccountInfoId,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage
        };
    }
}
