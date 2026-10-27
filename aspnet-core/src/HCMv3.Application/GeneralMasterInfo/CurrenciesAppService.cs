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
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the Currency master (ported from legacy XAF Currency business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Currencies)]
public class CurrenciesAppService : HCMv3AppServiceBase, ICurrenciesAppService
{
    private readonly IRepository<Currency> _currencyRepository;

    public CurrenciesAppService(IRepository<Currency> currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CurrencyDto>> GetAll(GetAllCurrenciesInput input)
    {
        input.Normalize();

        var query = _currencyRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CurrencyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCurrencyForEditOutput> GetCurrencyForEdit(EntityDto input)
    {
        var currency = await _currencyRepository.GetAsync(input.Id);

        return new GetCurrencyForEditOutput
        {
            Currency = ToCreateOrEditDto(currency)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCurrencyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Currencies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCurrencyDto input)
    {
        var currency = new Currency
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            DecimalCount = input.DecimalCount,
            CurrencyShortFormEn = input.CurrencyShortFormEn,
            CurrencyDenominationFormEn = input.CurrencyDenominationFormEn,
            CurrencyShortFormAr = input.CurrencyShortFormAr,
            CurrencyDenominationFormAr = input.CurrencyDenominationFormAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(currency);
        await ValidateUniquenessAsync(currency);

        await _currencyRepository.InsertAsync(currency);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Currencies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCurrencyDto input)
    {
        var currency = await _currencyRepository.GetAsync(input.Id.Value);

        currency.Code = input.Code;
        currency.NameEn = input.NameEn;
        currency.NameAr = input.NameAr;
        currency.DecimalCount = input.DecimalCount;
        currency.CurrencyShortFormEn = input.CurrencyShortFormEn;
        currency.CurrencyDenominationFormEn = input.CurrencyDenominationFormEn;
        currency.CurrencyShortFormAr = input.CurrencyShortFormAr;
        currency.CurrencyDenominationFormAr = input.CurrencyDenominationFormAr;

        await AssignCodeIfEmptyAsync(currency);
        await ValidateUniquenessAsync(currency);

        await _currencyRepository.UpdateAsync(currency);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Currencies_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _currencyRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Currency currency)
    {
        if (!currency.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _currencyRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        currency.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Currency currency)
    {
        var duplicateCode = await _currencyRepository.GetAll()
            .AnyAsync(c => c.Id != currency.Id && c.Code == currency.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _currencyRepository.GetAll()
            .AnyAsync(c => c.Id != currency.Id &&
                           (c.NameEn == currency.NameEn || c.NameAr == currency.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CurrencyDto ToDto(Currency currency)
    {
        return new CurrencyDto
        {
            Id = currency.Id,
            Code = currency.Code,
            NameEn = currency.NameEn,
            NameAr = currency.NameAr,
            DecimalCount = currency.DecimalCount,
            CurrencyShortFormEn = currency.CurrencyShortFormEn,
            CurrencyDenominationFormEn = currency.CurrencyDenominationFormEn,
            CurrencyShortFormAr = currency.CurrencyShortFormAr,
            CurrencyDenominationFormAr = currency.CurrencyDenominationFormAr
        };
    }

    private static CreateOrEditCurrencyDto ToCreateOrEditDto(Currency currency)
    {
        return new CreateOrEditCurrencyDto
        {
            Id = currency.Id,
            Code = currency.Code,
            NameEn = currency.NameEn,
            NameAr = currency.NameAr,
            DecimalCount = currency.DecimalCount,
            CurrencyShortFormEn = currency.CurrencyShortFormEn,
            CurrencyDenominationFormEn = currency.CurrencyDenominationFormEn,
            CurrencyShortFormAr = currency.CurrencyShortFormAr,
            CurrencyDenominationFormAr = currency.CurrencyDenominationFormAr
        };
    }
}
