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
/// Application service for the WageType master (ported from legacy XAF WageType business object). See
/// <see cref="WageType"/> class remarks for the Code non-auto-generation, create-time BillingType
/// restriction, and delete-time Category restriction.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypes)]
public class WageTypesAppService : HCMv3AppServiceBase, IWageTypesAppService
{
    private readonly IRepository<WageType> _wageTypeRepository;

    public WageTypesAppService(IRepository<WageType> wageTypeRepository)
    {
        _wageTypeRepository = wageTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageTypeDto>> GetAll(GetAllWageTypesInput input)
    {
        input.Normalize();

        var query = _wageTypeRepository.GetAll()
            .Include(w => w.ArrearSourceWageType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), w =>
                w.Code.Contains(input.Filter) ||
                w.NameEn.Contains(input.Filter) ||
                w.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageTypeForEditOutput> GetWageTypeForEdit(EntityDto input)
    {
        var entity = await _wageTypeRepository.GetAsync(input.Id);

        return new GetWageTypeForEditOutput
        {
            WageType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _wageTypeRepository.GetAsync(input.Id);

        if (entity.Category == Category.Technical)
        {
            throw new UserFriendlyException(L("TechnicalWageTypeCannotBeChangedOrDeleted"));
        }

        await _wageTypeRepository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageTypeDto input)
    {
        var entity = MapToEntity(input, new WageType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        if (entity.BillingType == BillingType.TimeType || entity.BillingType == BillingType.Provision)
        {
            throw new UserFriendlyException(L("BillingTypeReservedForInternalUse"));
        }

        await ValidateAsync(entity);

        await _wageTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageTypeDto input)
    {
        var entity = await _wageTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageTypeRepository.UpdateAsync(entity);
    }

    private static WageType MapToEntity(CreateOrEditWageTypeDto input, WageType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ShortNameEn = input.ShortNameEn;
        entity.ShortNameAr = input.ShortNameAr;
        entity.BillingType = input.BillingType;
        entity.BillingMethod = input.BillingMethod;
        entity.ProrateMethod = input.ProrateMethod;
        entity.FixedDays = input.FixedDays;
        entity.Rate = input.Rate;
        entity.Status = input.Status;
        entity.Category = input.Category;
        entity.ArrearSourceWageTypeId = input.ArrearSourceWageTypeId;
        entity.PayrollOrder = input.PayrollOrder;
        entity.IsSecured = input.IsSecured;
        entity.IncludeInIndemnity = input.IncludeInIndemnity;
        entity.IncludeInPaidLeave = input.IncludeInPaidLeave;
        entity.IncludeInBasicPay = input.IncludeInBasicPay;
        entity.IncludeInArrearPay = input.IncludeInArrearPay;
        entity.IncludeInCalculatedPay = input.IncludeInCalculatedPay;
        entity.IncludeInAdvancePay = input.IncludeInAdvancePay;
        entity.IncludeInOvertimePay = input.IncludeInOvertimePay;
        entity.IncludeInFIPosting = input.IncludeInFIPosting;
        entity.IncludeInCutoff = input.IncludeInCutoff;
        entity.IncludeInDisciplinary = input.IncludeInDisciplinary;
        entity.IncludeInVariablePay = input.IncludeInVariablePay;
        entity.IncludeInSocialSecurity = input.IncludeInSocialSecurity;
        entity.IncludeInPartialPay = input.IncludeInPartialPay;
        entity.IncludeInLateEarlyDed = input.IncludeInLateEarlyDed;
        entity.IncludeInServiceBonus = input.IncludeInServiceBonus;
        entity.IncludeInFund = input.IncludeInFund;
        entity.DontImpactNetPay = input.DontImpactNetPay;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code (required, never auto-generated), unique NameEn,
    /// PayrollOrder must be non-zero.
    /// </summary>
    private async Task ValidateAsync(WageType entity)
    {
        var duplicateCode = await _wageTypeRepository.GetAll()
            .AnyAsync(w => w.Id != entity.Id && w.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _wageTypeRepository.GetAll()
            .AnyAsync(w => w.Id != entity.Id && w.NameEn == entity.NameEn);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.PayrollOrder == 0)
        {
            throw new UserFriendlyException(L("PayrollOrderRequired"));
        }
    }

    private static WageTypeDto ToDto(WageType entity)
    {
        return new WageTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ShortNameEn = entity.ShortNameEn,
            ShortNameAr = entity.ShortNameAr,
            BillingType = entity.BillingType,
            BillingMethod = entity.BillingMethod,
            ProrateMethod = entity.ProrateMethod,
            FixedDays = entity.FixedDays,
            Rate = entity.Rate,
            Status = entity.Status,
            Category = entity.Category,
            ArrearSourceWageTypeId = entity.ArrearSourceWageTypeId,
            ArrearSourceWageTypeName = entity.ArrearSourceWageType?.NameEn,
            PayrollOrder = entity.PayrollOrder,
            IsSecured = entity.IsSecured,
            IncludeInIndemnity = entity.IncludeInIndemnity,
            IncludeInPaidLeave = entity.IncludeInPaidLeave,
            IncludeInBasicPay = entity.IncludeInBasicPay,
            IncludeInArrearPay = entity.IncludeInArrearPay,
            IncludeInCalculatedPay = entity.IncludeInCalculatedPay,
            IncludeInAdvancePay = entity.IncludeInAdvancePay,
            IncludeInOvertimePay = entity.IncludeInOvertimePay,
            IncludeInFIPosting = entity.IncludeInFIPosting,
            IncludeInCutoff = entity.IncludeInCutoff,
            IncludeInDisciplinary = entity.IncludeInDisciplinary,
            IncludeInVariablePay = entity.IncludeInVariablePay,
            IncludeInSocialSecurity = entity.IncludeInSocialSecurity,
            IncludeInPartialPay = entity.IncludeInPartialPay,
            IncludeInLateEarlyDed = entity.IncludeInLateEarlyDed,
            IncludeInServiceBonus = entity.IncludeInServiceBonus,
            IncludeInFund = entity.IncludeInFund,
            DontImpactNetPay = entity.DontImpactNetPay
        };
    }

    private static CreateOrEditWageTypeDto ToCreateOrEditDto(WageType entity)
    {
        return new CreateOrEditWageTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ShortNameEn = entity.ShortNameEn,
            ShortNameAr = entity.ShortNameAr,
            BillingType = entity.BillingType,
            BillingMethod = entity.BillingMethod,
            ProrateMethod = entity.ProrateMethod,
            FixedDays = entity.FixedDays,
            Rate = entity.Rate,
            Status = entity.Status,
            Category = entity.Category,
            ArrearSourceWageTypeId = entity.ArrearSourceWageTypeId,
            PayrollOrder = entity.PayrollOrder,
            IsSecured = entity.IsSecured,
            IncludeInIndemnity = entity.IncludeInIndemnity,
            IncludeInPaidLeave = entity.IncludeInPaidLeave,
            IncludeInBasicPay = entity.IncludeInBasicPay,
            IncludeInArrearPay = entity.IncludeInArrearPay,
            IncludeInCalculatedPay = entity.IncludeInCalculatedPay,
            IncludeInAdvancePay = entity.IncludeInAdvancePay,
            IncludeInOvertimePay = entity.IncludeInOvertimePay,
            IncludeInFIPosting = entity.IncludeInFIPosting,
            IncludeInCutoff = entity.IncludeInCutoff,
            IncludeInDisciplinary = entity.IncludeInDisciplinary,
            IncludeInVariablePay = entity.IncludeInVariablePay,
            IncludeInSocialSecurity = entity.IncludeInSocialSecurity,
            IncludeInPartialPay = entity.IncludeInPartialPay,
            IncludeInLateEarlyDed = entity.IncludeInLateEarlyDed,
            IncludeInServiceBonus = entity.IncludeInServiceBonus,
            IncludeInFund = entity.IncludeInFund,
            DontImpactNetPay = entity.DontImpactNetPay
        };
    }
}
