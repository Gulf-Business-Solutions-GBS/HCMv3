using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for the employee-membership child of <see cref="RosterGroup"/> (ported from legacy
/// XAF RosterGroupDetail business object), listed separately and linked back via
/// <see cref="RosterGroupDetail.RosterGroupId"/>.
///
/// The legacy <c>PerformValidation</c> only checks RosterGroup/StartDate/EndDate presence and
/// EndDate &gt;= StartDate — no EndDate-auto-chaining logic exists in the legacy file. An additional
/// overlap check (same RosterGroup + Employee, overlapping date range) is added here, following the same
/// pattern used elsewhere in this codebase (e.g. EmployeeContractsAppService) for date-ranged child
/// entities, since the legacy file doesn't otherwise define this scenario.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupDetails)]
public class RosterGroupDetailsAppService : HCMv3AppServiceBase, IRosterGroupDetailsAppService
{
    private readonly IRepository<RosterGroupDetail> _rosterGroupDetailRepository;

    public RosterGroupDetailsAppService(IRepository<RosterGroupDetail> rosterGroupDetailRepository)
    {
        _rosterGroupDetailRepository = rosterGroupDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterGroupDetailDto>> GetAll(GetAllRosterGroupDetailsInput input)
    {
        input.Normalize();

        var query = _rosterGroupDetailRepository.GetAll()
            .Include(r => r.RosterGroup)
            .Include(r => r.Employee)
            .WhereIf(input.RosterGroupId.HasValue, r => r.RosterGroupId == input.RosterGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterGroupDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterGroupDetailForEditOutput> GetRosterGroupDetailForEdit(EntityDto input)
    {
        var entity = await _rosterGroupDetailRepository.GetAsync(input.Id);

        return new GetRosterGroupDetailForEditOutput
        {
            RosterGroupDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterGroupDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterGroupDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterGroupDetailDto input)
    {
        var entity = MapToEntity(input, new RosterGroupDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _rosterGroupDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterGroupDetailDto input)
    {
        var entity = await _rosterGroupDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _rosterGroupDetailRepository.UpdateAsync(entity);
    }

    private static RosterGroupDetail MapToEntity(CreateOrEditRosterGroupDetailDto input, RosterGroupDetail entity)
    {
        entity.RosterGroupId = input.RosterGroupId;
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: RosterGroup required, StartDate/EndDate required, EndDate
    /// not earlier than StartDate — plus an additional overlap check (see class remarks).
    /// </summary>
    private async Task ValidateAsync(RosterGroupDetail entity)
    {
        if (entity.RosterGroupId <= 0)
        {
            throw new UserFriendlyException(L("RosterGroupCannotBeBlank"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlaps = await _rosterGroupDetailRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.RosterGroupId == entity.RosterGroupId &&
                           r.EmployeeId == entity.EmployeeId &&
                           r.StartDate.Date <= entity.EndDate.Date &&
                           r.EndDate.Date >= entity.StartDate.Date);
        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static RosterGroupDetailDto ToDto(RosterGroupDetail entity)
    {
        return new RosterGroupDetailDto
        {
            Id = entity.Id,
            RosterGroupId = entity.RosterGroupId,
            RosterGroupName = entity.RosterGroup?.NameEn,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditRosterGroupDetailDto ToCreateOrEditDto(RosterGroupDetail entity)
    {
        return new CreateOrEditRosterGroupDetailDto
        {
            Id = entity.Id,
            RosterGroupId = entity.RosterGroupId,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
