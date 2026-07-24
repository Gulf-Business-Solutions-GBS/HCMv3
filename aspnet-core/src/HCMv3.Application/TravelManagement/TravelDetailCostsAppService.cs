using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="TravelDetailCost"/>, the aggregated child of <see cref="TravelDetail"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.TravelDetailCost</c>).
/// Straightforward CRUD only -- see <see cref="TravelDetailCost"/> class remarks for the dropped legacy
/// TotalAmount/TotalTravelCost recompute cascade.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts)]
public class TravelDetailCostsAppService : HCMv3AppServiceBase, ITravelDetailCostsAppService
{
    private readonly IRepository<TravelDetailCost> _repository;

    public TravelDetailCostsAppService(IRepository<TravelDetailCost> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelDetailCostDto>> GetAll(GetAllTravelDetailCostsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.ExpenseType)
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
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelDetailCostDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TravelDetailCost MapToEntity(CreateOrEditTravelDetailCostDto input, TravelDetailCost entity)
    {
        entity.TravelDetailId = input.TravelDetailId;
        entity.ExpenseTypeId = input.ExpenseTypeId;
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
            Amount = entity.Amount,
            Comments = entity.Comments
        };
    }
}
