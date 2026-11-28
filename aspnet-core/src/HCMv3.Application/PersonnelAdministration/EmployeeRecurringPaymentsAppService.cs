using System;
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
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeRecurringPayment"/> (ported from legacy XAF
/// EmployeeRecurringPayment business object). Ports legacy <c>OnSaving</c>/<c>PerformValidations</c>: required
/// fields, Amount &gt; 0, EndDate &gt;= StartDate, no overlapping StartDate/EndDate window for the same
/// Employee+WageType (with legacy's detailed message), the auto-EndDate chaining when
/// ManagePayrollSettings.EnableEffectiveDateInRecPayment is on, and the retro-date rules: an edit that only moves
/// the End Date recalculates payroll from the day after the end date instead of from the Start Date, and a row
/// closed by the chaining raises no recalculation of its own.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments)]
public class EmployeeRecurringPaymentsAppService : HCMv3AppServiceBase, IEmployeeRecurringPaymentsAppService
{
    private static readonly DateTime InfiniteEndDate = new(9999, 12, 31);

    private readonly IRepository<EmployeeRecurringPayment> _repository;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IPayrollRetroTrigger _retroTrigger;

    public EmployeeRecurringPaymentsAppService(
        IRepository<EmployeeRecurringPayment> repository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IPayrollRetroTrigger retroTrigger)
    {
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _retroTrigger = retroTrigger;
    }

    /// <summary>
    /// Every employee of the tenant, as a small projection, for the Employee dropdown on the Recurring Payments
    /// list (where the employee is chosen in the dialog). Shorter codes first, so numeric codes sort as numbers.
    /// </summary>
    [HttpPost]
    public async Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeRecurringPaymentEmployeesInput input)
    {
        var filter = input?.Filter?.Trim();

        var items = await _employeeRepository.GetAll()
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

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRecurringPaymentDto>> GetAll(GetAllEmployeeRecurringPaymentsInput input)
    {
        input.Normalize();
        var filter = input.Filter?.Trim();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Employee.Code.Contains(filter) ||
                e.Employee.FullNameEn.Contains(filter) ||
                e.Employee.FullNameAr.Contains(filter) ||
                e.WageType.NameEn.Contains(filter) ||
                e.WageType.NameAr.Contains(filter));

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

        // Legacy's prevXxx values, as loaded.
        var previousEmployeeId = entity.EmployeeId;
        var previousWageTypeId = entity.WageTypeId;
        var previousStartDate = entity.StartDate.Date;
        var previousAmount = entity.Amount;
        var previousEndDate = entity.EndDate.Date;

        MapToEntity(input, entity);

        // Legacy OnSaving "Set EarliestMasterDataChange" (Ticket #885, RCF): when only the End Date moved --
        // typically to stop the payment -- nothing before the end date changed, so payroll recalculates from the
        // day after it rather than from the Start Date.
        var onlyEndDateChanged = entity.EmployeeId == previousEmployeeId
            && entity.WageTypeId == previousWageTypeId
            && entity.StartDate.Date == previousStartDate
            && entity.Amount == previousAmount
            && entity.EndDate.Date != previousEndDate;

        // Marked BEFORE validating: validation reads settings, which can flush the unit of work and save this row
        // early. Unmarked, that save records the Start Date, and the later end-date retro can no longer move it.
        if (onlyEndDateChanged)
        {
            _retroTrigger.MarkStopRetroEffect(entity);
        }

        await ValidateEntityAsync(entity);

        if (onlyEndDateChanged)
        {
            _retroTrigger.SetRetroDate(typeof(EmployeeRecurringPayment), entity.EmployeeId,
                EndDateRetroFrom(previousEndDate, entity.EndDate.Date), entity.Id);
        }

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        // Legacy OnDeleting recalculates from the deleted row's Start Date -- what the payroll retro hook does
        // for every deleted payroll-sensitive row.
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// The first day whose pay an End Date change affects: the day after the EARLIER of the old and new end dates.
    /// Legacy used the new end date only, so lengthening a payment (say March to June) recalculated from July and
    /// April-June were never paid. The earlier of the two covers shortening and lengthening alike, and is legacy's
    /// own date whenever a payment is shortened or stopped.
    /// </summary>
    public static DateTime EndDateRetroFrom(DateTime previousEndDate, DateTime newEndDate)
    {
        var earlier = previousEndDate.Date < newEndDate.Date ? previousEndDate.Date : newEndDate.Date;
        return earlier >= InfiniteEndDate ? InfiniteEndDate : earlier.AddDays(1);
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

        var overlap = await _repository.GetAll()
            .Where(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.WageTypeId == entity.WageTypeId
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date)
            .OrderBy(e => e.StartDate)
            .FirstOrDefaultAsync();
        if (overlap != null)
        {
            var (employee, wageType) = await DescribeAsync(entity);
            throw new UserFriendlyException(L("RecurringPaymentOverlapFound",
                employee, wageType, Day(overlap.StartDate), Day(overlap.EndDate)));
        }
    }

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
            var (employee, wageType) = await DescribeAsync(entity);
            throw new UserFriendlyException(L("RecurringPaymentMoreThanOneOverlap", employee, wageType));
        }

        if (overlapping.Count == 1)
        {
            var previous = overlapping[0];
            if (previous.EndDate.Date != InfiniteEndDate)
            {
                var (employee, wageType) = await DescribeAsync(entity);
                throw new UserFriendlyException(L("RecurringPaymentLastEntryMustBeOpenEnded",
                    employee, wageType, Day(previous.StartDate), Day(previous.EndDate)));
            }

            if (previous.StartDate.Date >= entity.StartDate.Date)
            {
                var (employee, wageType) = await DescribeAsync(entity);
                throw new UserFriendlyException(L("RecurringPaymentLastEntryStartDateMustBeEarlier",
                    employee, wageType, Day(previous.StartDate), Day(entity.StartDate)));
            }

            // Legacy closes the previous entry with its own save, whose end-date-only branch recalculates from the
            // day after its new end date -- this entry's Start Date, which this entry's own save already raises.
            // So the re-closed row must not add a recalculation from its own, older, Start Date.
            previous.EndDate = entity.StartDate.Date.AddDays(-1);
            _retroTrigger.MarkEndDateAutoChange(previous);
            await _repository.UpdateAsync(previous);
        }
    }

    private static string Day(DateTime date) =>
        date.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Legacy names the employee (code and English name) and the wage type in every overlap message.</summary>
    private async Task<(string Employee, string WageType)> DescribeAsync(EmployeeRecurringPayment entity)
    {
        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == entity.EmployeeId)
            .Select(e => e.Code + " - " + e.FullNameEn)
            .FirstOrDefaultAsync();
        var wageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Id == entity.WageTypeId)
            .Select(w => w.NameEn)
            .FirstOrDefaultAsync();
        return (employee, wageType);
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
