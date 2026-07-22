using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the CurrencyConvertor rate record (ported from legacy XAF CurrencyConvertor
/// business object). Pure rate record (no Code/NameEn/NameAr): the legacy <c>PerformValidation</c>
/// required non-null From/To currencies, a non-default calendar date, and a non-zero rate -- ported as
/// explicit business-rule checks below since Data Annotations can't express "not zero"/"not default"
/// cleanly. Follows the same dual-FK-to-<see cref="Currency"/> pattern as <see cref="PayeeBankDetail"/>'s
/// dual FK to <c>Bank</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors)]
public class CurrencyConvertorsAppService : HCMv3AppServiceBase, ICurrencyConvertorsAppService
{
    private readonly IRepository<CurrencyConvertor> _currencyConvertorRepository;

    public CurrencyConvertorsAppService(IRepository<CurrencyConvertor> currencyConvertorRepository)
    {
        _currencyConvertorRepository = currencyConvertorRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CurrencyConvertorDto>> GetAll(GetAllCurrencyConvertorsInput input)
    {
        input.Normalize();

        var query = _currencyConvertorRepository.GetAll()
            .Include(c => c.FromCurr)
            .Include(c => c.ToCurr);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CurrencyConvertorDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCurrencyConvertorForEditOutput> GetCurrencyConvertorForEdit(EntityDto input)
    {
        var entity = await _currencyConvertorRepository.GetAsync(input.Id);

        return new GetCurrencyConvertorForEditOutput
        {
            CurrencyConvertor = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCurrencyConvertorDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCurrencyConvertorDto input)
    {
        var entity = new CurrencyConvertor
        {
            CalendarDate = input.CalendarDate,
            FromCurrId = input.FromCurrId,
            ToCurrId = input.ToCurrId,
            Rate = input.Rate,
            TenantId = AbpSession.TenantId ?? 0
        };

        ValidateBusinessRules(entity);

        await _currencyConvertorRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCurrencyConvertorDto input)
    {
        var entity = await _currencyConvertorRepository.GetAsync(input.Id.Value);

        entity.CalendarDate = input.CalendarDate;
        entity.FromCurrId = input.FromCurrId;
        entity.ToCurrId = input.ToCurrId;
        entity.Rate = input.Rate;

        ValidateBusinessRules(entity);

        await _currencyConvertorRepository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _currencyConvertorRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: non-default calendar date, both currencies set, non-zero rate.
    /// </summary>
    private void ValidateBusinessRules(CurrencyConvertor entity)
    {
        if (entity.CalendarDate == default)
        {
            throw new UserFriendlyException(L("CalendarDateCannotBeBlank"));
        }

        if (entity.FromCurrId == null)
        {
            throw new UserFriendlyException(L("FromCurrencyCannotBeBlank"));
        }

        if (entity.ToCurrId == null)
        {
            throw new UserFriendlyException(L("ToCurrencyCannotBeBlank"));
        }

        if (entity.Rate == 0)
        {
            throw new UserFriendlyException(L("RateCannotBeZero"));
        }
    }

    private static CurrencyConvertorDto ToDto(CurrencyConvertor entity)
    {
        return new CurrencyConvertorDto
        {
            Id = entity.Id,
            CalendarDate = entity.CalendarDate,
            FromCurrId = entity.FromCurrId,
            FromCurrName = entity.FromCurr?.NameEn,
            ToCurrId = entity.ToCurrId,
            ToCurrName = entity.ToCurr?.NameEn,
            Rate = entity.Rate
        };
    }

    private static CreateOrEditCurrencyConvertorDto ToCreateOrEditDto(CurrencyConvertor entity)
    {
        return new CreateOrEditCurrencyConvertorDto
        {
            Id = entity.Id,
            CalendarDate = entity.CalendarDate,
            FromCurrId = entity.FromCurrId,
            ToCurrId = entity.ToCurrId,
            Rate = entity.Rate
        };
    }
}
