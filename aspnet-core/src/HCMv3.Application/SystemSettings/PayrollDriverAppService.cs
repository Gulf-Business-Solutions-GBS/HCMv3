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
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="PayrollDriver"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.PayrollDriver</c>). Legacy <c>PerformValidation</c> real
/// business rules -- Country is required, and at most one <see cref="PayrollDriver"/> row per Country -- are
/// ported to <see cref="ValidateAsync"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollDrivers)]
public class PayrollDriverAppService : HCMv3AppServiceBase, IPayrollDriverAppService
{
    private readonly IRepository<PayrollDriver> _repository;
    private readonly IRepository<Country> _countryRepository;

    public PayrollDriverAppService(IRepository<PayrollDriver> repository, IRepository<Country> countryRepository)
    {
        _repository = repository;
        _countryRepository = countryRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayrollDriverListDto>> GetPayrollDrivers(GetPayrollDriversInput input)
    {
        var query = from d in _repository.GetAll()
                     join c in _countryRepository.GetAll() on d.CountryId equals c.Id
                     select new { Driver = d, CountryName = c.NameEn };

        query = query.WhereIf(!input.Filter.IsNullOrWhiteSpace(), x => x.CountryName.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "CountryName" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollDriverListDto>(
            totalCount,
            items.Select(x => new PayrollDriverListDto
            {
                Id = x.Driver.Id,
                CountryId = x.Driver.CountryId,
                CountryName = x.CountryName,
                LeaveDeductionStartMonth = x.Driver.LeaveDeductionStartMonth
            }).ToList());
    }

    public async Task<PayrollDriverEditDto> GetPayrollDriverForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollDrivers_Create, AppPermissions.Pages_SystemSettings_PayrollDrivers_Edit)]
    public async Task CreateOrEditPayrollDriver(PayrollDriverEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollDrivers_Delete)]
    public async Task DeletePayrollDriver(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task CreateAsync(PayrollDriverEditDto input)
    {
        var entity = MapToEntity(input, new PayrollDriver());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(PayrollDriverEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Country is required, and at most one <see cref="PayrollDriver"/>
    /// row may exist per Country.
    /// </summary>
    private async Task ValidateAsync(PayrollDriver entity)
    {
        if (entity.CountryId == 0)
        {
            throw new UserFriendlyException(L("CountryShouldNotBeBlank"));
        }

        var duplicateCountry = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.CountryId == entity.CountryId);
        if (duplicateCountry)
        {
            throw new UserFriendlyException(L("PayrollDriverRecordAlreadyExistsForCountry"));
        }
    }

    private static PayrollDriver MapToEntity(PayrollDriverEditDto input, PayrollDriver entity)
    {
        entity.CountryId = input.CountryId;
        entity.LeaveDeductionStartMonth = input.LeaveDeductionStartMonth;

        return entity;
    }

    private static PayrollDriverEditDto ToEditDto(PayrollDriver entity)
    {
        return new PayrollDriverEditDto
        {
            Id = entity.Id,
            CountryId = entity.CountryId,
            LeaveDeductionStartMonth = entity.LeaveDeductionStartMonth
        };
    }
}
