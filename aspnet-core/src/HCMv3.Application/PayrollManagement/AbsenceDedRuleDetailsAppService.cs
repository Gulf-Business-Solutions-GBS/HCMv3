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

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceDedRuleDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
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
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling detail under the same parent rule (the legacy source ran this overlap check twice with
    /// equivalent predicates — only run once here).
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
