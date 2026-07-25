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
using HCMv3.ManpowerPlanningManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Application service for <see cref="ManpowerPlanningPeriod"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanningPeriod</c>). Legacy
/// <c>PerformValidation</c> parity (required Name/StartDate/EndDate, EndDate &gt;= StartDate) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods)]
public class ManpowerPlanningPeriodsAppService : HCMv3AppServiceBase, IManpowerPlanningPeriodsAppService
{
    private readonly IRepository<ManpowerPlanningPeriod> _periodRepository;

    public ManpowerPlanningPeriodsAppService(IRepository<ManpowerPlanningPeriod> periodRepository)
    {
        _periodRepository = periodRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ManpowerPlanningPeriodDto>> GetAll(GetAllManpowerPlanningPeriodsInput input)
    {
        input.Normalize();

        var query = _periodRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p => p.Name.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ManpowerPlanningPeriodDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetManpowerPlanningPeriodForEditOutput> GetManpowerPlanningPeriodForEdit(EntityDto input)
    {
        var entity = await _periodRepository.GetAsync(input.Id);

        return new GetManpowerPlanningPeriodForEditOutput
        {
            ManpowerPlanningPeriod = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditManpowerPlanningPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _periodRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditManpowerPlanningPeriodDto input)
    {
        var entity = MapToEntity(input, new ManpowerPlanningPeriod());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _periodRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditManpowerPlanningPeriodDto input)
    {
        var entity = await _periodRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _periodRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate cannot be earlier than StartDate.
    /// </summary>
    private void ValidateDates(ManpowerPlanningPeriod entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static ManpowerPlanningPeriod MapToEntity(CreateOrEditManpowerPlanningPeriodDto input, ManpowerPlanningPeriod entity)
    {
        entity.Name = input.Name;
        entity.Active = input.Active;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    private static ManpowerPlanningPeriodDto ToDto(ManpowerPlanningPeriod entity)
    {
        return new ManpowerPlanningPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Active = entity.Active,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditManpowerPlanningPeriodDto ToCreateOrEditDto(ManpowerPlanningPeriod entity)
    {
        return new CreateOrEditManpowerPlanningPeriodDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Active = entity.Active,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
