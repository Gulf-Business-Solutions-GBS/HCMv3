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
/// Application service for the AccountInfo master (ported from legacy XAF AccountInfo business object). See
/// <see cref="AccountInfo"/> class remarks for why <c>CodeWithNameEn</c> is not ported as a stored property.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AccountInfos)]
public class AccountInfosAppService : HCMv3AppServiceBase, IAccountInfosAppService
{
    private readonly IRepository<AccountInfo> _accountInfoRepository;

    public AccountInfosAppService(IRepository<AccountInfo> accountInfoRepository)
    {
        _accountInfoRepository = accountInfoRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AccountInfoDto>> GetAll(GetAllAccountInfosInput input)
    {
        input.Normalize();

        var query = _accountInfoRepository.GetAll()
            .Include(a => a.AccountType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.NameEn.Contains(input.Filter) ||
                a.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AccountInfoDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAccountInfoForEditOutput> GetAccountInfoForEdit(EntityDto input)
    {
        var entity = await _accountInfoRepository.GetAsync(input.Id);

        return new GetAccountInfoForEditOutput
        {
            AccountInfo = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAccountInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AccountInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _accountInfoRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AccountInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAccountInfoDto input)
    {
        var entity = MapToEntity(input, new AccountInfo());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accountInfoRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AccountInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAccountInfoDto input)
    {
        var entity = await _accountInfoRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _accountInfoRepository.UpdateAsync(entity);
    }

    private static AccountInfo MapToEntity(CreateOrEditAccountInfoDto input, AccountInfo entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.AccountTypeId = input.AccountTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AccountInfo entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _accountInfoRepository.GetAll()
            .Where(a => a.Code != null && a.Code != "")
            .Select(a => a.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique NameEn/NameAr, and AccountTypeId
    /// required.
    /// </summary>
    private async Task ValidateAsync(AccountInfo entity)
    {
        var duplicateCode = await _accountInfoRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _accountInfoRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           (a.NameEn == entity.NameEn || a.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.AccountTypeId <= 0)
        {
            throw new UserFriendlyException(L("AccountTypeRequired"));
        }
    }

    private static AccountInfoDto ToDto(AccountInfo entity)
    {
        return new AccountInfoDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            AccountTypeId = entity.AccountTypeId,
            AccountTypeName = entity.AccountType?.NameEn
        };
    }

    private static CreateOrEditAccountInfoDto ToCreateOrEditDto(AccountInfo entity)
    {
        return new CreateOrEditAccountInfoDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            AccountTypeId = entity.AccountTypeId
        };
    }
}
