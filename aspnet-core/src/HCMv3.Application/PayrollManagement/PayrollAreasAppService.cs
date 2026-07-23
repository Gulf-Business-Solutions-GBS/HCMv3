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
/// Application service for the PayrollArea master (ported from legacy XAF PayrollArea business object). See
/// <see cref="PayrollArea"/> class remarks for the dropped NotificationConfig FKs and default-currency
/// simplification.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollAreas)]
public class PayrollAreasAppService : HCMv3AppServiceBase, IPayrollAreasAppService
{
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public PayrollAreasAppService(IRepository<PayrollArea> payrollAreaRepository)
    {
        _payrollAreaRepository = payrollAreaRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayrollAreaDto>> GetAll(GetAllPayrollAreasInput input)
    {
        input.Normalize();

        var query = _payrollAreaRepository.GetAll()
            .Include(a => a.PayrollPeriod)
            .Include(a => a.Currency)
            .Include(a => a.FICompany)
            .Include(a => a.FIBank)
            .Include(a => a.Country)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollAreaDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPayrollAreaForEditOutput> GetPayrollAreaForEdit(EntityDto input)
    {
        var entity = await _payrollAreaRepository.GetAsync(input.Id);

        return new GetPayrollAreaForEditOutput
        {
            PayrollArea = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayrollAreaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollAreas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _payrollAreaRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollAreas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayrollAreaDto input)
    {
        var entity = MapToEntity(input, new PayrollArea());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _payrollAreaRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollAreas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayrollAreaDto input)
    {
        var entity = await _payrollAreaRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _payrollAreaRepository.UpdateAsync(entity);
    }

    private static PayrollArea MapToEntity(CreateOrEditPayrollAreaDto input, PayrollArea entity)
    {
        entity.Code = input.Code;
        entity.Name = input.Name;
        entity.PayrollPeriodId = input.PayrollPeriodId;
        entity.CurrencyId = input.CurrencyId;
        entity.CutOffDay = input.CutOffDay;
        entity.FICompanyId = input.FICompanyId;
        entity.FIBankId = input.FIBankId;
        entity.CountryId = input.CountryId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(PayrollArea entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _payrollAreaRepository.GetAll()
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
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique Name, PayrollPeriodId and CurrencyId
    /// required.
    /// </summary>
    private async Task ValidateAsync(PayrollArea entity)
    {
        var duplicateCode = await _payrollAreaRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _payrollAreaRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Name == entity.Name);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.PayrollPeriodId <= 0)
        {
            throw new UserFriendlyException(L("PayrollPeriodRequired"));
        }

        if (entity.CurrencyId <= 0)
        {
            throw new UserFriendlyException(L("CurrencyRequired"));
        }
    }

    private static PayrollAreaDto ToDto(PayrollArea entity)
    {
        return new PayrollAreaDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            PayrollPeriodId = entity.PayrollPeriodId,
            PayrollPeriodName = entity.PayrollPeriod?.NameEn,
            CurrencyId = entity.CurrencyId,
            CurrencyName = entity.Currency?.NameEn,
            CutOffDay = entity.CutOffDay,
            FICompanyId = entity.FICompanyId,
            FICompanyName = entity.FICompany?.NameEn,
            FIBankId = entity.FIBankId,
            FIBankName = entity.FIBank?.NameEn,
            CountryId = entity.CountryId,
            CountryName = entity.Country?.NameEn
        };
    }

    private static CreateOrEditPayrollAreaDto ToCreateOrEditDto(PayrollArea entity)
    {
        return new CreateOrEditPayrollAreaDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            PayrollPeriodId = entity.PayrollPeriodId,
            CurrencyId = entity.CurrencyId,
            CutOffDay = entity.CutOffDay,
            FICompanyId = entity.FICompanyId,
            FIBankId = entity.FIBankId,
            CountryId = entity.CountryId
        };
    }
}
