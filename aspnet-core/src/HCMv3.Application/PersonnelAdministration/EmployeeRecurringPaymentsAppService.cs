using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeRecurringPayment"/> (ported from legacy XAF
/// EmployeeRecurringPayment business object, Wave 4 deferred bucket). Ports the essential active
/// validation only: required fields, Amount &gt; 0, EndDate &gt;= StartDate, and no overlapping
/// StartDate/EndDate window for the same Employee+WageType, and the legacy auto-EndDate chaining when
/// ManagePayrollSettings.EnableEffectiveDateInRecPayment is on. The retro-calculation cascade is NOT ported -- see
/// <see cref="EmployeeRecurringPayment"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments)]
public class EmployeeRecurringPaymentsAppService : HCMv3AppServiceBase, IEmployeeRecurringPaymentsAppService
{
    private readonly IRepository<EmployeeRecurringPayment> _repository;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public EmployeeRecurringPaymentsAppService(
        IRepository<EmployeeRecurringPayment> repository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRecurringPaymentDto>> GetAll(GetAllEmployeeRecurringPaymentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeRecurringPaymentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeRecurringPaymentForEditOutput> GetEmployeeRecurringPaymentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeRecurringPaymentForEditOutput
        {
            EmployeeRecurringPayment = new CreateOrEditEmployeeRecurringPaymentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                Comments = entity.Comments,
                PayInterval = entity.PayInterval
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeRecurringPaymentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeRecurringPaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeeRecurringPayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRecurringPaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeRecurringPayment entity)
    {
        if (entity.Amount <= 0)
        {
            throw new UserFriendlyException(L("AmountMustBeGreaterThanZero"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var chainOpenEnded = payrollSettings != null && payrollSettings.EnableEffectiveDateInRecPayment
                             && entity.EndDate.Date == InfiniteEndDate;

        if (chainOpenEnded)
        {
            await ChainOpenEndedEntryAsync(entity);
            return;
        }

        var hasOverlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.WageTypeId == entity.WageTypeId
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date);
        if (hasOverlap)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static readonly DateTime InfiniteEndDate = new(9999, 12, 31);

    /// <summary>
    /// Legacy <c>EmployeeRecurringPayment.OnSaving</c> "Auto EndDate" (Zoho #2783, RHH): with
    /// <c>ManagePayrollSettings.EnableEffectiveDateInRecPayment</c> on, a new open-ended entry may overlap
    /// exactly one existing entry, which must itself be open-ended and start earlier; that entry is then
    /// closed the day before the new one starts. Anything else is refused with legacy's reasons.
    /// </summary>
    private async Task ChainOpenEndedEntryAsync(EmployeeRecurringPayment entity)
    {
        var overlapping = await _repository.GetAll()
            .Where(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.WageTypeId == entity.WageTypeId
                        && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date)
            .ToListAsync();

        if (overlapping.Count > 1)
        {
            throw new UserFriendlyException(L("RecurringPaymentMoreThanOneOverlap"));
        }

        if (overlapping.Count == 1)
        {
            var previous = overlapping[0];
            if (previous.EndDate.Date != InfiniteEndDate)
            {
                throw new UserFriendlyException(L("RecurringPaymentLastEntryMustBeOpenEnded",
                    previous.StartDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    previous.EndDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)));
            }

            if (previous.StartDate.Date >= entity.StartDate.Date)
            {
                throw new UserFriendlyException(L("RecurringPaymentLastEntryStartDateMustBeEarlier",
                    entity.StartDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)));
            }

            previous.EndDate = entity.StartDate.Date.AddDays(-1);
            await _repository.UpdateAsync(previous);
        }
    }

    private static EmployeeRecurringPayment MapToEntity(CreateOrEditEmployeeRecurringPaymentDto input, EmployeeRecurringPayment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;
        entity.PayInterval = input.PayInterval;
        return entity;
    }

    private static EmployeeRecurringPaymentDto ToDto(EmployeeRecurringPayment entity)
    {
        return new EmployeeRecurringPaymentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            Amount = entity.Amount,
            Comments = entity.Comments,
            PayInterval = entity.PayInterval
        };
    }
}
