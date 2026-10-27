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
/// Application service for the absence deduction rule header (ported from legacy XAF AbsenceDeductionRule
/// business object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules)]
public class AbsenceDeductionRulesAppService : HCMv3AppServiceBase, IAbsenceDeductionRulesAppService
{
    private readonly IRepository<AbsenceDeductionRule> _absenceDeductionRuleRepository;

    public AbsenceDeductionRulesAppService(IRepository<AbsenceDeductionRule> absenceDeductionRuleRepository)
    {
        _absenceDeductionRuleRepository = absenceDeductionRuleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceDeductionRuleDto>> GetAll(GetAllAbsenceDeductionRulesInput input)
    {
        input.Normalize();

        var query = _absenceDeductionRuleRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.RuleName.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceDeductionRuleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceDeductionRuleForEditOutput> GetAbsenceDeductionRuleForEdit(EntityDto input)
    {
        var entity = await _absenceDeductionRuleRepository.GetAsync(input.Id);

        return new GetAbsenceDeductionRuleForEditOutput
        {
            AbsenceDeductionRule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceDeductionRuleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceDeductionRuleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceDeductionRuleDto input)
    {
        var entity = MapToEntity(input, new AbsenceDeductionRule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _absenceDeductionRuleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceDeductionRuleDto input)
    {
        var entity = await _absenceDeductionRuleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _absenceDeductionRuleRepository.UpdateAsync(entity);
    }

    private static AbsenceDeductionRule MapToEntity(CreateOrEditAbsenceDeductionRuleDto input, AbsenceDeductionRule entity)
    {
        entity.Code = input.Code;
        entity.RuleName = input.RuleName;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AbsenceDeductionRule entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _absenceDeductionRuleRepository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique RuleName.
    /// </summary>
    private async Task ValidateAsync(AbsenceDeductionRule entity)
    {
        var duplicateCode = await _absenceDeductionRuleRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _absenceDeductionRuleRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.RuleName == entity.RuleName);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static AbsenceDeductionRuleDto ToDto(AbsenceDeductionRule entity)
    {
        return new AbsenceDeductionRuleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            RuleName = entity.RuleName
        };
    }

    private static CreateOrEditAbsenceDeductionRuleDto ToCreateOrEditDto(AbsenceDeductionRule entity)
    {
        return new CreateOrEditAbsenceDeductionRuleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            RuleName = entity.RuleName
        };
    }
}
