using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the BudgetingGroupRateDetail detail record (ported from legacy XAF
/// BudgetingGroupRateDetail). Pure detail record (no Code/NameEn/NameAr): plain CRUD without
/// auto-code or duplicate validation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails)]
public class BudgetingGroupRateDetailsAppService : HCMv3AppServiceBase, IBudgetingGroupRateDetailsAppService
{
    private readonly IRepository<BudgetingGroupRateDetail> _repository;

    public BudgetingGroupRateDetailsAppService(IRepository<BudgetingGroupRateDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BudgetingGroupRateDetailDto>> GetAll(GetAllBudgetingGroupRateDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.BudgetingGroup);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BudgetingGroupRateDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBudgetingGroupRateDetailForEditOutput> GetBudgetingGroupRateDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetBudgetingGroupRateDetailForEditOutput
        {
            BudgetingGroupRateDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBudgetingGroupRateDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBudgetingGroupRateDetailDto input)
    {
        var entity = new BudgetingGroupRateDetail
        {
            BudgetingGroupId = input.BudgetingGroupId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            BonusRate = input.BonusRate,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBudgetingGroupRateDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.BudgetingGroupId = input.BudgetingGroupId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.BonusRate = input.BonusRate;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static BudgetingGroupRateDetailDto ToDto(BudgetingGroupRateDetail entity)
    {
        return new BudgetingGroupRateDetailDto
        {
            Id = entity.Id,
            BudgetingGroupId = entity.BudgetingGroupId,
            BudgetingGroupName = entity.BudgetingGroup?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            BonusRate = entity.BonusRate
        };
    }

    private static CreateOrEditBudgetingGroupRateDetailDto ToCreateOrEditDto(BudgetingGroupRateDetail entity)
    {
        return new CreateOrEditBudgetingGroupRateDetailDto
        {
            Id = entity.Id,
            BudgetingGroupId = entity.BudgetingGroupId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            BonusRate = entity.BonusRate
        };
    }
}
