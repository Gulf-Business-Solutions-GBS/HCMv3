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
/// Application service for <see cref="IndemnityPayRule"/> (ported from legacy XAF IndemnityPayRule business
/// object). Its child rows (<see cref="IndemnityPayRuleDetail"/>) are listed/edited separately via
/// <see cref="IndemnityPayRuleDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRules)]
public class IndemnityPayRulesAppService : HCMv3AppServiceBase, IIndemnityPayRulesAppService
{
    private readonly IRepository<IndemnityPayRule> _indemnityPayRuleRepository;

    public IndemnityPayRulesAppService(IRepository<IndemnityPayRule> indemnityPayRuleRepository)
    {
        _indemnityPayRuleRepository = indemnityPayRuleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<IndemnityPayRuleDto>> GetAll(GetAllIndemnityPayRulesInput input)
    {
        input.Normalize();

        var query = _indemnityPayRuleRepository.GetAll()
            .Include(r => r.Country);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<IndemnityPayRuleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetIndemnityPayRuleForEditOutput> GetIndemnityPayRuleForEdit(EntityDto input)
    {
        var entity = await _indemnityPayRuleRepository.GetAsync(input.Id);

        return new GetIndemnityPayRuleForEditOutput
        {
            IndemnityPayRule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditIndemnityPayRuleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _indemnityPayRuleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditIndemnityPayRuleDto input)
    {
        var entity = MapToEntity(input, new IndemnityPayRule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _indemnityPayRuleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditIndemnityPayRuleDto input)
    {
        var entity = await _indemnityPayRuleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _indemnityPayRuleRepository.UpdateAsync(entity);
    }

    private static IndemnityPayRule MapToEntity(CreateOrEditIndemnityPayRuleDto input, IndemnityPayRule entity)
    {
        entity.SeparationType = input.SeparationType;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.MaxNumber = input.MaxNumber;
        entity.MaxUnit = input.MaxUnit;
        entity.MaxPayPercentage = input.MaxPayPercentage;
        entity.CountryId = input.CountryId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlap with another
    /// IndemnityPayRule for the same SeparationType.
    /// </summary>
    private async Task ValidateAsync(IndemnityPayRule entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _indemnityPayRuleRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.SeparationType == entity.SeparationType &&
                           r.StartDate.Date <= entity.EndDate.Date &&
                           r.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static IndemnityPayRuleDto ToDto(IndemnityPayRule entity)
    {
        return new IndemnityPayRuleDto
        {
            Id = entity.Id,
            SeparationType = entity.SeparationType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            MaxNumber = entity.MaxNumber,
            MaxUnit = entity.MaxUnit,
            MaxPayPercentage = entity.MaxPayPercentage,
            CountryId = entity.CountryId,
            CountryName = entity.Country?.NameEn
        };
    }

    private static CreateOrEditIndemnityPayRuleDto ToCreateOrEditDto(IndemnityPayRule entity)
    {
        return new CreateOrEditIndemnityPayRuleDto
        {
            Id = entity.Id,
            SeparationType = entity.SeparationType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            MaxNumber = entity.MaxNumber,
            MaxUnit = entity.MaxUnit,
            MaxPayPercentage = entity.MaxPayPercentage,
            CountryId = entity.CountryId
        };
    }
}
