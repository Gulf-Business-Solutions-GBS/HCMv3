using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="PATimeline"/>. The legacy in-memory activity-type-ordering check
/// (<c>PerformValidation</c>'s "ActivityType should be in order") is not reproduced -- deliberate
/// simplification; only the duplicate Period+ActivityType and StartDate/EndDate range checks are enforced.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelines)]
public class PATimelinesAppService : HCMv3AppServiceBase, IPATimelinesAppService
{
    private readonly IRepository<PATimeline> _repository;

    public PATimelinesAppService(IRepository<PATimeline> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PATimelineDto>> GetAll(GetAllPATimelinesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.PAPeriod)
            .WhereIf(input.PAPeriodId.HasValue, e => e.PAPeriodId == input.PAPeriodId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PATimelineDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPATimelineForEditOutput> GetPATimelineForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPATimelineForEditOutput { PATimeline = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPATimelineDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelines_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelines_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPATimelineDto input)
    {
        var entity = new PATimeline
        {
            TenantId = AbpSession.TenantId ?? 0,
            PAPeriodId = input.PAPeriodId,
            ActivityType = input.ActivityType,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PATimelines_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPATimelineDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.PAPeriodId = input.PAPeriodId;
        entity.ActivityType = input.ActivityType;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(PATimeline entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.PAPeriodId == entity.PAPeriodId
                           && e.ActivityType == entity.ActivityType);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PATimelineAlreadyExistsForPeriodAndActivityType"));
        }
    }

    private static PATimelineDto ToDto(PATimeline entity)
    {
        return new PATimelineDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            PAPeriodName = entity.PAPeriod?.Name,
            ActivityType = entity.ActivityType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditPATimelineDto ToCreateOrEditDto(PATimeline entity)
    {
        return new CreateOrEditPATimelineDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            ActivityType = entity.ActivityType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
