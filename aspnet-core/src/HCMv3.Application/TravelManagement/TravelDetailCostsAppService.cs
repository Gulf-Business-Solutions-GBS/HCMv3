using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.TravelManagement.Dto;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="TravelDetailCost"/>, the aggregated child of <see cref="TravelDetail"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetailCost</c>).
/// Every create, update and delete recalculates the leg's TotalAmount and the trip's TotalTravelCost through
/// <see cref="ITravelCostRollupService"/> (legacy <c>TravelDetailCost.UpdateTotalAmount</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts)]
public class TravelDetailCostsAppService : HCMv3AppServiceBase, ITravelDetailCostsAppService
{
    private readonly IRepository<TravelDetailCost> _repository;
    private readonly IRepository<TravelDetail> _travelDetailRepository;
    private readonly IRepository<Travel> _travelRepository;
    private readonly ITravelCostRollupService _costRollupService;

    public TravelDetailCostsAppService(
        IRepository<TravelDetailCost> repository,
        IRepository<TravelDetail> travelDetailRepository,
        IRepository<Travel> travelRepository,
        ITravelCostRollupService costRollupService)
    {
        _repository = repository;
        _travelDetailRepository = travelDetailRepository;
        _travelRepository = travelRepository;
        _costRollupService = costRollupService;
    }

    /// <summary>
    /// The cost lines, scoped to legs of trips the caller can see -- the same two-hop semi-join, and for the
    /// same reason, as <c>TravelDetailsAppService.GetAll</c>. A cost line is two levels below the row that
    /// carries the approval status, and it hides when that row does because it is only ever reached through
    /// it.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<TravelDetailCostDto>> GetAll(GetAllTravelDetailCostsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var visibleTravelIds = _travelRepository.GetAll().Select(t => t.Id);

            var visibleDetailIds = _travelDetailRepository.GetAll()
                .Where(d => visibleTravelIds.Contains(d.TravelId))
                .Select(d => d.Id);

            var query = _repository.GetAll()
                .Include(c => c.ExpenseType)
                .Where(c => visibleDetailIds.Contains(c.TravelDetailId))
                .WhereIf(input.TravelDetailId.HasValue, c => c.TravelDetailId == input.TravelDetailId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<TravelDetailCostDto>(
                totalCount,
                items.Select(ToDto).ToList());
        }
    }

    public async Task<GetTravelDetailCostForEditOutput> GetTravelDetailCostForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelDetailCostForEditOutput
        {
            TravelDetailCost = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelDetailCostDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelDetailCostDto input)
    {
        var entity = MapToEntity(input, new TravelDetailCost());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);

        // Legacy TravelDetailCost.OnSaving -> UpdateTotalAmount(0) (TravelDetailCost.cs lines 85, 111-133).
        await CurrentUnitOfWork.SaveChangesAsync();
        await _costRollupService.RecalculateTravelDetailTotalAsync(entity.TravelDetailId);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelDetailCostDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);

        // Legacy TravelDetailCost.cs lines 85, 111-133. Only the leg the line belongs to NOW is recalculated:
        // a line moved to another leg leaves the old leg's total as it was, exactly as in legacy.
        await CurrentUnitOfWork.SaveChangesAsync();
        await _costRollupService.RecalculateTravelDetailTotalAsync(entity.TravelDetailId);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Delete)]
    public async Task Delete(EntityDto input)
    {
        var travelDetailId = await _repository.GetAll()
            .Where(c => c.Id == input.Id)
            .Select(c => (int?)c.TravelDetailId)
            .FirstOrDefaultAsync();

        await _repository.DeleteAsync(input.Id);

        // Legacy TravelDetailCost.OnDeleting -> UpdateTotalAmount(this.Oid) (TravelDetailCost.cs lines 109,
        // 117): the leg total without this line, then the trip.
        if (travelDetailId.HasValue)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
            await _costRollupService.RecalculateTravelDetailTotalAsync(travelDetailId.Value);
        }
    }

    private static TravelDetailCost MapToEntity(CreateOrEditTravelDetailCostDto input, TravelDetailCost entity)
    {
        entity.TravelDetailId = input.TravelDetailId;
        entity.ExpenseTypeId = input.ExpenseTypeId;
        entity.CurrencyId = input.CurrencyId;
        entity.Amount = input.Amount;
        entity.Comments = input.Comments;

        return entity;
    }

    private static TravelDetailCostDto ToDto(TravelDetailCost entity)
    {
        return new TravelDetailCostDto
        {
            Id = entity.Id,
            TravelDetailId = entity.TravelDetailId,
            ExpenseTypeId = entity.ExpenseTypeId,
            ExpenseTypeNameEn = entity.ExpenseType?.NameEn,
            CurrencyId = entity.CurrencyId,
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTravelDetailCostDto ToCreateOrEditDto(TravelDetailCost entity)
    {
        return new CreateOrEditTravelDetailCostDto
        {
            Id = entity.Id,
            TravelDetailId = entity.TravelDetailId,
            ExpenseTypeId = entity.ExpenseTypeId,
            CurrencyId = entity.CurrencyId,
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }
}
