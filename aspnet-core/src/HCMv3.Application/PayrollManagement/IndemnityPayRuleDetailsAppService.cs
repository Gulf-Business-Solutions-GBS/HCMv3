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
/// Application service for the year-of-service pay-percentage step of an <see cref="IndemnityPayRule"/>
/// (ported from legacy XAF IndemnityPayRuleDetail business object), listed separately and linked back via
/// <see cref="IndemnityPayRuleDetail.IndemnityPayRuleId"/>. Note: the legacy overlap check ran twice with
/// equivalent predicates — only run once here (same simplification as
/// <c>EmployeeSubGroupFundsAppService</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails)]
public class IndemnityPayRuleDetailsAppService : HCMv3AppServiceBase, IIndemnityPayRuleDetailsAppService
{
    private readonly IRepository<IndemnityPayRuleDetail> _indemnityPayRuleDetailRepository;

    public IndemnityPayRuleDetailsAppService(IRepository<IndemnityPayRuleDetail> indemnityPayRuleDetailRepository)
    {
        _indemnityPayRuleDetailRepository = indemnityPayRuleDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<IndemnityPayRuleDetailDto>> GetAll(GetAllIndemnityPayRuleDetailsInput input)
    {
        input.Normalize();

        var query = _indemnityPayRuleDetailRepository.GetAll()
            .Include(d => d.IndemnityPayRule)
            .WhereIf(input.IndemnityPayRuleId.HasValue, d => d.IndemnityPayRuleId == input.IndemnityPayRuleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<IndemnityPayRuleDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetIndemnityPayRuleDetailForEditOutput> GetIndemnityPayRuleDetailForEdit(EntityDto input)
    {
        var entity = await _indemnityPayRuleDetailRepository.GetAsync(input.Id);

        return new GetIndemnityPayRuleDetailForEditOutput
        {
            IndemnityPayRuleDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditIndemnityPayRuleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _indemnityPayRuleDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditIndemnityPayRuleDetailDto input)
    {
        var entity = MapToEntity(input, new IndemnityPayRuleDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _indemnityPayRuleDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditIndemnityPayRuleDetailDto input)
    {
        var entity = await _indemnityPayRuleDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _indemnityPayRuleDetailRepository.UpdateAsync(entity);
    }

    private static IndemnityPayRuleDetail MapToEntity(CreateOrEditIndemnityPayRuleDetailDto input, IndemnityPayRuleDetail entity)
    {
        entity.IndemnityPayRuleId = input.IndemnityPayRuleId;
        entity.MinYearOfService = input.MinYearOfService;
        entity.MaxYearOfService = input.MaxYearOfService;
        entity.Number = input.Number;
        entity.Unit = input.Unit;
        entity.PayPercentage = input.PayPercentage;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: MaxYearOfService &gt;= MinYearOfService and no overlap with a
    /// sibling row under the same IndemnityPayRule.
    /// </summary>
    private async Task ValidateAsync(IndemnityPayRuleDetail entity)
    {
        if (entity.MaxYearOfService < entity.MinYearOfService)
        {
            throw new UserFriendlyException(L("MaxYearOfServiceCannotBeLessThanMinYearOfService"));
        }

        var overlap = await _indemnityPayRuleDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.IndemnityPayRuleId == entity.IndemnityPayRuleId &&
                           d.MinYearOfService <= entity.MaxYearOfService &&
                           d.MaxYearOfService >= entity.MinYearOfService);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static IndemnityPayRuleDetailDto ToDto(IndemnityPayRuleDetail entity)
    {
        return new IndemnityPayRuleDetailDto
        {
            Id = entity.Id,
            IndemnityPayRuleId = entity.IndemnityPayRuleId,
            MinYearOfService = entity.MinYearOfService,
            MaxYearOfService = entity.MaxYearOfService,
            Number = entity.Number,
            Unit = entity.Unit,
            PayPercentage = entity.PayPercentage
        };
    }

    private static CreateOrEditIndemnityPayRuleDetailDto ToCreateOrEditDto(IndemnityPayRuleDetail entity)
    {
        return new CreateOrEditIndemnityPayRuleDetailDto
        {
            Id = entity.Id,
            IndemnityPayRuleId = entity.IndemnityPayRuleId,
            MinYearOfService = entity.MinYearOfService,
            MaxYearOfService = entity.MaxYearOfService,
            Number = entity.Number,
            Unit = entity.Unit,
            PayPercentage = entity.PayPercentage
        };
    }
}
