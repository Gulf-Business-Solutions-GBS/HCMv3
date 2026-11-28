using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeePayment"/>, the legacy (LHRMS) Payment transaction screen.
/// Ports the legacy business object in full:
/// <list type="bullet">
/// <item><description>Required Employee, Wage Type, Currency and Payment Date (legacy <c>PerformValidations</c>);
/// an empty currency takes the default one, as legacy <c>AfterConstruction</c> did.</description></item>
/// <item><description>Read-only Billing Type (the wage type's) and Expected Amount
/// (<see cref="IExpectedPaymentAmountService"/>), both non-persistent.</description></item>
/// <item><description>The Employee lookup (<see cref="IEmployeePaymentEligibilityService"/>) and the Wage Type
/// lookup criteria from the legacy detail view.</description></item>
/// </list>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments)]
public class EmployeePaymentsAppService : HCMv3AppServiceBase, IEmployeePaymentsAppService
{
    private readonly IRepository<EmployeePayment> _repository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IExpectedPaymentAmountService _expectedPaymentAmountService;
    private readonly IEmployeePaymentEligibilityService _eligibilityService;
    private readonly ICurrencyService _currencyService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public EmployeePaymentsAppService(
        IRepository<EmployeePayment> repository,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IExpectedPaymentAmountService expectedPaymentAmountService,
        IEmployeePaymentEligibilityService eligibilityService,
        ICurrencyService currencyService,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _expectedPaymentAmountService = expectedPaymentAmountService;
        _eligibilityService = eligibilityService;
        _currencyService = currencyService;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeePaymentDto>> GetAll(GetAllEmployeePaymentsInput input)
    {
        input.Normalize();
        var filter = input.Filter?.Trim();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .Include(e => e.Currency)
            .Include(e => e.CostCenter)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Employee.Code.Contains(filter) ||
                e.Employee.FullNameEn.Contains(filter) ||
                e.Employee.FullNameAr.Contains(filter) ||
                e.WageType.NameEn.Contains(filter) ||
                e.WageType.Code.Contains(filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = new List<EmployeePaymentDto>(items.Count);
        foreach (var item in items)
        {
            var dto = ToDto(item);
            dto.ExpectedAmount = await _expectedPaymentAmountService.GetExpectedAmountAsync(
                item.EmployeeId, item.WageTypeId, item.PaymentDate, item.UnitType, item.Amount);
            dtos.Add(dto);
        }

        return new PagedResultDto<EmployeePaymentDto>(totalCount, dtos);
    }

    public async Task<GetEmployeePaymentForEditOutput> GetEmployeePaymentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .FirstOrDefaultAsync(e => e.Id == input.Id)
            ?? throw new EntityNotFoundException(typeof(EmployeePayment), input.Id);

        return new GetEmployeePaymentForEditOutput
        {
            EmployeePayment = new CreateOrEditEmployeePaymentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                CurrencyId = entity.CurrencyId,
                PaymentDate = entity.PaymentDate,
                Comments = entity.Comments,
                UnitType = entity.UnitType,
                CostCenterId = entity.CostCenterId
            },
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeNameEn = entity.WageType?.NameEn,
            BillingType = entity.WageType?.BillingType.ToString(),
            ExpectedAmount = await _expectedPaymentAmountService.GetExpectedAmountAsync(
                entity.EmployeeId, entity.WageTypeId, entity.PaymentDate, entity.UnitType, entity.Amount)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeePaymentDto input)
    {
        await ValidateAndDefaultAsync(input);

        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeePaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeePayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeePaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [HttpPost]
    public async Task<double> GetExpectedAmount(GetExpectedPaymentAmountInput input)
    {
        return await _expectedPaymentAmountService.GetExpectedAmountAsync(
            input.EmployeeId, input.WageTypeId, input.PaymentDate, input.UnitType, input.Amount);
    }

    [HttpPost]
    public async Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeePaymentEmployeesInput input)
    {
        var allowedIds = await _eligibilityService.GetAllowedEmployeeIdsAsync();
        var filter = input?.Filter?.Trim();

        var items = await _employeeRepository.GetAll()
            .Where(e => allowedIds.Contains(e.Id))
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(filter) ||
                e.FullNameEn.Contains(filter) ||
                e.FullNameAr.Contains(filter))
            .OrderBy(e => e.Code.Length)
            .ThenBy(e => e.Code)
            .Select(e => new SimpleLookupDto
            {
                Id = e.Id,
                Code = e.Code,
                NameEn = e.FullNameEn,
                NameAr = e.FullNameAr
            })
            .ToListAsync();

        return new ListResultDto<SimpleLookupDto>(items);
    }

    public async Task<ListResultDto<EmployeePaymentWageTypeLookupDto>> GetWageTypesForLookup()
    {
        // Legacy EmployeePayment_DetailView WageType criteria:
        // [IncludeInBasicPay] = False Or [IncludeInArrearPay] = False Or [IncludeInCalculatedPay] = False
        var wageTypes = await _wageTypeRepository.GetAll()
            .Where(w => !w.IncludeInBasicPay || !w.IncludeInArrearPay || !w.IncludeInCalculatedPay)
            .OrderBy(w => w.Code)
            .ToListAsync();

        return new ListResultDto<EmployeePaymentWageTypeLookupDto>(wageTypes
            .Select(w => new EmployeePaymentWageTypeLookupDto
            {
                Id = w.Id,
                Code = w.Code,
                NameEn = w.NameEn,
                BillingType = w.BillingType.ToString()
            })
            .ToList());
    }

    public async Task<EmployeePaymentScreenSettingsDto> GetScreenSettings()
    {
        var currency = await _currencyService.GetDefaultCurrencyAsync();
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        return new EmployeePaymentScreenSettingsDto
        {
            DefaultCurrencyId = currency?.Id,
            ShowBillingType = payrollSettings?.ShowBillingTypeInPaymentScreen ?? false
        };
    }

    /// <summary>Legacy <c>PerformValidations</c>, after the default currency of <c>AfterConstruction</c>.</summary>
    private async Task ValidateAndDefaultAsync(CreateOrEditEmployeePaymentDto input)
    {
        if (input.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (input.WageTypeId <= 0)
        {
            throw new UserFriendlyException(L("WageTypeCannotBeBlank"));
        }

        if (!input.CurrencyId.HasValue || input.CurrencyId.Value <= 0)
        {
            input.CurrencyId = (await _currencyService.GetDefaultCurrencyAsync())?.Id;
        }

        if (!input.CurrencyId.HasValue)
        {
            throw new UserFriendlyException(L("CurrencyCannotBeBlank"));
        }

        if (input.PaymentDate == default)
        {
            throw new UserFriendlyException(L("PaymentDateCannotBeBlank"));
        }
    }

    private static EmployeePayment MapToEntity(CreateOrEditEmployeePaymentDto input, EmployeePayment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.Comments = input.Comments;
        entity.UnitType = input.UnitType;
        entity.CostCenterId = input.CostCenterId;
        return entity;
    }

    private static EmployeePaymentDto ToDto(EmployeePayment entity)
    {
        return new EmployeePaymentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            BillingType = entity.WageType?.BillingType.ToString(),
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyNameEn = entity.Currency?.NameEn,
            PaymentDate = entity.PaymentDate,
            Comments = entity.Comments,
            UnitType = entity.UnitType,
            CostCenterId = entity.CostCenterId,
            CostCenterNameEn = entity.CostCenter?.NameEn
        };
    }
}
