using System;
using System.Globalization;
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
/// Application service for the date-range deduction step of an <see cref="AbsenceDeductionRule"/> (ported
/// from legacy XAF AbsenceDedRuleDetail business object), listed separately and linked back via
/// <see cref="AbsenceDedRuleDetail.AbsenceDeductionRuleId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails)]
public class AbsenceDedRuleDetailsAppService : HCMv3AppServiceBase, IAbsenceDedRuleDetailsAppService
{
    private readonly IRepository<AbsenceDedRuleDetail> _absenceDedRuleDetailRepository;

    public AbsenceDedRuleDetailsAppService(IRepository<AbsenceDedRuleDetail> absenceDedRuleDetailRepository)
    {
        _absenceDedRuleDetailRepository = absenceDedRuleDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceDedRuleDetailDto>> GetAll(GetAllAbsenceDedRuleDetailsInput input)
    {
        input.Normalize();

        var query = _absenceDedRuleDetailRepository.GetAll()
            .Include(d => d.AbsenceDeductionRule)
            .WhereIf(input.AbsenceDeductionRuleId.HasValue, d => d.AbsenceDeductionRuleId == input.AbsenceDeductionRuleId);

        query = ApplyFilter(query, input.Filter);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceDedRuleDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    /// <summary>See <see cref="GetAllAbsenceDedRuleDetailsInput.Filter"/>. Text that is none of those matches nothing.</summary>
    private static IQueryable<AbsenceDedRuleDetail> ApplyFilter(IQueryable<AbsenceDedRuleDetail> query, string filter)
    {
        filter = filter?.Trim();
        if (string.IsNullOrEmpty(filter))
        {
            return query;
        }

        if (filter.Length == 4 && int.TryParse(filter, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year >= 1900)
        {
            var yearStart = new DateTime(year, 1, 1);
            var nextYear = yearStart.AddYears(1);
            return query.Where(d => d.StartDate < nextYear && d.EndDate >= yearStart);
        }

        if (DateTime.TryParseExact(filter, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return query.Where(d => d.StartDate <= date && d.EndDate >= date);
        }

        if (double.TryParse(filter, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return query.Where(d => d.DaysCount == number || d.DeductionNumber == number);
        }

        return query.Where(d => false);
    }

    public async Task<GetAbsenceDedRuleDetailForEditOutput> GetAbsenceDedRuleDetailForEdit(EntityDto input)
    {
        var entity = await _absenceDedRuleDetailRepository.GetAsync(input.Id);

        return new GetAbsenceDedRuleDetailForEditOutput
        {
            AbsenceDedRuleDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceDedRuleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceDedRuleDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceDedRuleDetailDto input)
    {
        var entity = MapToEntity(input, new AbsenceDedRuleDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _absenceDedRuleDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceDedRuleDetailDto input)
    {
        var entity = await _absenceDedRuleDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _absenceDedRuleDetailRepository.UpdateAsync(entity);
    }

    private static AbsenceDedRuleDetail MapToEntity(CreateOrEditAbsenceDedRuleDetailDto input, AbsenceDedRuleDetail entity)
    {
        entity.AbsenceDeductionRuleId = input.AbsenceDeductionRuleId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.DaysCount = input.DaysCount;
        entity.DeductionUnit = input.DeductionUnit;
        entity.DeductionNumber = input.DeductionNumber;

        return entity;
    }

    /// <summary>
    /// EndDate &gt;= StartDate, and no overlapping date range with another version of the same SLAB -- a sibling
    /// detail under the same rule with the same <c>DaysCount</c>.
    ///
    /// <para><b>Deliberately narrower than legacy.</b> Legacy <c>PerformValidation</c> refused any date overlap
    /// under one rule (it even ran the check twice). But the engine reads a rule as several slabs in force at
    /// the same time -- <c>EmployeeLeavePayCalculationService.GetActualSickLeaveDaysAsync</c> picks the first
    /// detail whose DaysCount covers the running count of unpaid sick days -- so legacy's check made every
    /// multi-slab rule, Kuwait's own art. 69 sick leave among them, impossible to enter. Slabs now share dates;
    /// only two versions of the same slab are kept apart.</para>
    /// </summary>
    private async Task ValidateAsync(AbsenceDedRuleDetail entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _absenceDedRuleDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.AbsenceDeductionRuleId == entity.AbsenceDeductionRuleId &&
                           d.DaysCount == entity.DaysCount &&
                           d.StartDate.Date <= entity.EndDate.Date &&
                           d.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static AbsenceDedRuleDetailDto ToDto(AbsenceDedRuleDetail entity)
    {
        return new AbsenceDedRuleDetailDto
        {
            Id = entity.Id,
            AbsenceDeductionRuleId = entity.AbsenceDeductionRuleId,
            AbsenceDeductionRuleName = entity.AbsenceDeductionRule?.RuleName,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DaysCount = entity.DaysCount,
            DeductionUnit = entity.DeductionUnit,
            DeductionNumber = entity.DeductionNumber
        };
    }

    private static CreateOrEditAbsenceDedRuleDetailDto ToCreateOrEditDto(AbsenceDedRuleDetail entity)
    {
        return new CreateOrEditAbsenceDedRuleDetailDto
        {
            Id = entity.Id,
            AbsenceDeductionRuleId = entity.AbsenceDeductionRuleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            DaysCount = entity.DaysCount,
            DeductionUnit = entity.DeductionUnit,
            DeductionNumber = entity.DeductionNumber
        };
    }
}
