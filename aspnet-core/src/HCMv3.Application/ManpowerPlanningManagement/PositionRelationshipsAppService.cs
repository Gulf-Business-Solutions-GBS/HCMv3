using System;
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
/// Application service for <see cref="PositionRelationship"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.PositionRelationship</c>). Legacy
/// <c>PerformValidation</c> (required fields, EndDate &gt;= StartDate, FTE-limit overlap check) and
/// <c>UpdatePositionStatus</c> (recompute <see cref="Position.Vacant"/>) are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships)]
public class PositionRelationshipsAppService : HCMv3AppServiceBase, IPositionRelationshipsAppService
{
    private readonly IRepository<PositionRelationship> _positionRelationshipRepository;
    private readonly IRepository<Position> _positionRepository;

    public PositionRelationshipsAppService(
        IRepository<PositionRelationship> positionRelationshipRepository,
        IRepository<Position> positionRepository)
    {
        _positionRelationshipRepository = positionRelationshipRepository;
        _positionRepository = positionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PositionRelationshipDto>> GetAll(GetAllPositionRelationshipsInput input)
    {
        input.Normalize();

        var query = _positionRelationshipRepository.GetAll()
            .Include(r => r.Position)
            .Include(r => r.Employee)
            .WhereIf(input.PositionId.HasValue, r => r.PositionId == input.PositionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PositionRelationshipDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPositionRelationshipForEditOutput> GetPositionRelationshipForEdit(EntityDto input)
    {
        var entity = await _positionRelationshipRepository.GetAsync(input.Id);

        return new GetPositionRelationshipForEditOutput
        {
            PositionRelationship = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPositionRelationshipDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _positionRelationshipRepository.GetAsync(input.Id);
        var positionId = entity.PositionId;

        await _positionRelationshipRepository.DeleteAsync(input.Id);

        await UpdatePositionVacancyAsync(positionId, excludeRelationshipId: input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPositionRelationshipDto input)
    {
        var entity = MapToEntity(input, new PositionRelationship());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _positionRelationshipRepository.InsertAsync(entity);

        await UpdatePositionVacancyAsync(entity.PositionId, excludeRelationshipId: 0);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPositionRelationshipDto input)
    {
        var entity = await _positionRelationshipRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _positionRelationshipRepository.UpdateAsync(entity);

        await UpdatePositionVacancyAsync(entity.PositionId, excludeRelationshipId: 0);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate, and the position's FTE limit is not
    /// exceeded by overlapping (StartDate/EndDate ranges intersect), non-deleted relationships held by
    /// *other* employees.
    /// </summary>
    private async Task ValidateAsync(PositionRelationship entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var position = await _positionRepository.GetAsync(entity.PositionId);

        var overlappingCount = await _positionRelationshipRepository.GetAll()
            .Where(r => r.Id != entity.Id
                        && r.EmployeeId != entity.EmployeeId
                        && r.PositionId == entity.PositionId
                        && !(r.StartDate.Date > entity.EndDate.Date || r.EndDate.Date < entity.StartDate.Date))
            .CountAsync();

        if (overlappingCount + 1 > position.FTELimit)
        {
            throw new UserFriendlyException(L("PositionFTELimitExceeded", position.FTELimit));
        }
    }

    /// <summary>
    /// Legacy <c>UpdatePositionStatus</c> parity: a position is Vacant iff there is no non-deleted
    /// relationship whose date range currently includes today.
    /// </summary>
    private async Task UpdatePositionVacancyAsync(int positionId, int excludeRelationshipId)
    {
        var today = DateTime.Now.Date;

        var hasCurrentHolder = await _positionRelationshipRepository.GetAll()
            .AnyAsync(r => r.Id != excludeRelationshipId
                           && r.PositionId == positionId
                           && r.StartDate.Date <= today
                           && r.EndDate.Date >= today);

        var position = await _positionRepository.GetAsync(positionId);
        position.Vacant = !hasCurrentHolder;
        await _positionRepository.UpdateAsync(position);
    }

    private static PositionRelationship MapToEntity(CreateOrEditPositionRelationshipDto input, PositionRelationship entity)
    {
        entity.PositionId = input.PositionId;
        entity.RelationType = input.RelationType;
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    private static PositionRelationshipDto ToDto(PositionRelationship entity)
    {
        return new PositionRelationshipDto
        {
            Id = entity.Id,
            PositionId = entity.PositionId,
            PositionCodeWithNameEn = entity.Position == null ? null : (entity.Position.Code ?? "") + "-" + (entity.Position.NameEn ?? ""),
            RelationType = entity.RelationType,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditPositionRelationshipDto ToCreateOrEditDto(PositionRelationship entity)
    {
        return new CreateOrEditPositionRelationshipDto
        {
            Id = entity.Id,
            PositionId = entity.PositionId,
            RelationType = entity.RelationType,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
