using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ManpowerPlanningManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Application service for <see cref="ManpowerPlanningDetail"/>, the aggregated child of
/// <see cref="ManpowerPlanning"/> (ported from legacy XAF ManpowerPlanningDetail business object). Legacy
/// <c>PerformValidation</c> (required JobTitle; if Position is set, each non-zero month must fall within
/// the position's Min/Max salary range) and <c>UpdateTotalAmount</c> (recompute this row's
/// <see cref="ManpowerPlanningDetail.TotalAmount"/> and the parent's
/// <see cref="ManpowerPlanning.GrandTotalAmount"/>) are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails)]
public class ManpowerPlanningDetailsAppService : HCMv3AppServiceBase, IManpowerPlanningDetailsAppService
{
    private readonly IRepository<ManpowerPlanningDetail> _detailRepository;
    private readonly IRepository<ManpowerPlanning> _manpowerPlanningRepository;
    private readonly IRepository<Position> _positionRepository;

    public ManpowerPlanningDetailsAppService(
        IRepository<ManpowerPlanningDetail> detailRepository,
        IRepository<ManpowerPlanning> manpowerPlanningRepository,
        IRepository<Position> positionRepository)
    {
        _detailRepository = detailRepository;
        _manpowerPlanningRepository = manpowerPlanningRepository;
        _positionRepository = positionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ManpowerPlanningDetailDto>> GetAll(GetAllManpowerPlanningDetailsInput input)
    {
        input.Normalize();

        var query = _detailRepository.GetAll()
            .Include(d => d.JobTitle)
            .Include(d => d.Position)
            .Include(d => d.Holder)
            .WhereIf(input.ManpowerPlanningId.HasValue, d => d.ManpowerPlanningId == input.ManpowerPlanningId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ManpowerPlanningDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetManpowerPlanningDetailForEditOutput> GetManpowerPlanningDetailForEdit(EntityDto input)
    {
        var entity = await _detailRepository.GetAsync(input.Id);

        return new GetManpowerPlanningDetailForEditOutput
        {
            ManpowerPlanningDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditManpowerPlanningDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _detailRepository.GetAsync(input.Id);
        var manpowerPlanningId = entity.ManpowerPlanningId;

        await _detailRepository.DeleteAsync(input.Id);

        await RecalculateGrandTotalAsync(manpowerPlanningId, excludeDetailId: input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditManpowerPlanningDetailDto input)
    {
        var entity = MapToEntity(input, new ManpowerPlanningDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateSalaryRangeAsync(entity);
        entity.TotalAmount = CalculateTotal(entity);

        await _detailRepository.InsertAsync(entity);

        await RecalculateGrandTotalAsync(entity.ManpowerPlanningId, excludeDetailId: 0);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditManpowerPlanningDetailDto input)
    {
        var entity = await _detailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateSalaryRangeAsync(entity);
        entity.TotalAmount = CalculateTotal(entity);

        await _detailRepository.UpdateAsync(entity);

        await RecalculateGrandTotalAsync(entity.ManpowerPlanningId, excludeDetailId: 0);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: when a <see cref="Position"/> is already linked, every
    /// non-zero month amount must fall within that position's Min/Max salary range.
    /// </summary>
    private async Task ValidateSalaryRangeAsync(ManpowerPlanningDetail entity)
    {
        if (!entity.PositionId.HasValue)
        {
            return;
        }

        var position = await _positionRepository.GetAsync(entity.PositionId.Value);

        var months = new (string Name, double Amount)[]
        {
            ("January", entity.January), ("February", entity.February), ("March", entity.March),
            ("April", entity.April), ("May", entity.May), ("June", entity.June),
            ("July", entity.July), ("August", entity.August), ("September", entity.September),
            ("October", entity.October), ("November", entity.November), ("December", entity.December)
        };

        foreach (var (name, amount) in months)
        {
            if (amount != 0 && (position.MinSalary > amount || position.MaxSalary < amount))
            {
                throw new UserFriendlyException(L("SalaryOutOfPositionRange", name, position.MinSalary, position.MaxSalary));
            }
        }
    }

    private static double CalculateTotal(ManpowerPlanningDetail entity)
    {
        return entity.January + entity.February + entity.March
             + entity.April + entity.May + entity.June
             + entity.July + entity.August + entity.September
             + entity.October + entity.November + entity.December;
    }

    /// <summary>
    /// Legacy <c>UpdateTotalAmount</c> parity: recompute the parent's <see cref="ManpowerPlanning.GrandTotalAmount"/>
    /// as the sum of all its details' <see cref="ManpowerPlanningDetail.TotalAmount"/> (excluding the row
    /// being deleted, if any).
    /// </summary>
    private async Task RecalculateGrandTotalAsync(int manpowerPlanningId, int excludeDetailId)
    {
        var grandTotal = await _detailRepository.GetAll()
            .Where(d => d.ManpowerPlanningId == manpowerPlanningId && d.Id != excludeDetailId)
            .SumAsync(d => (double?)d.TotalAmount) ?? 0;

        var manpowerPlanning = await _manpowerPlanningRepository.GetAsync(manpowerPlanningId);
        manpowerPlanning.GrandTotalAmount = grandTotal;
        await _manpowerPlanningRepository.UpdateAsync(manpowerPlanning);
    }

    private static ManpowerPlanningDetail MapToEntity(CreateOrEditManpowerPlanningDetailDto input, ManpowerPlanningDetail entity)
    {
        entity.ManpowerPlanningId = input.ManpowerPlanningId;
        entity.JobTitleId = input.JobTitleId;
        entity.HolderId = input.HolderId;
        entity.January = input.January;
        entity.February = input.February;
        entity.March = input.March;
        entity.April = input.April;
        entity.May = input.May;
        entity.June = input.June;
        entity.July = input.July;
        entity.August = input.August;
        entity.September = input.September;
        entity.October = input.October;
        entity.November = input.November;
        entity.December = input.December;

        return entity;
    }

    private static ManpowerPlanningDetailDto ToDto(ManpowerPlanningDetail entity)
    {
        return new ManpowerPlanningDetailDto
        {
            Id = entity.Id,
            ManpowerPlanningId = entity.ManpowerPlanningId,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            PositionId = entity.PositionId,
            PositionCodeWithNameEn = entity.Position == null ? null : (entity.Position.Code ?? "") + "-" + (entity.Position.NameEn ?? ""),
            HolderId = entity.HolderId,
            HolderFullNameEn = entity.Holder?.FullNameEn,
            January = entity.January,
            February = entity.February,
            March = entity.March,
            April = entity.April,
            May = entity.May,
            June = entity.June,
            July = entity.July,
            August = entity.August,
            September = entity.September,
            October = entity.October,
            November = entity.November,
            December = entity.December,
            TotalAmount = entity.TotalAmount
        };
    }

    private static CreateOrEditManpowerPlanningDetailDto ToCreateOrEditDto(ManpowerPlanningDetail entity)
    {
        return new CreateOrEditManpowerPlanningDetailDto
        {
            Id = entity.Id,
            ManpowerPlanningId = entity.ManpowerPlanningId,
            JobTitleId = entity.JobTitleId,
            HolderId = entity.HolderId,
            January = entity.January,
            February = entity.February,
            March = entity.March,
            April = entity.April,
            May = entity.May,
            June = entity.June,
            July = entity.July,
            August = entity.August,
            September = entity.September,
            October = entity.October,
            November = entity.November,
            December = entity.December
        };
    }
}
