using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeExtdWorkLocation"/> (ported from legacy XAF
/// EmployeeExtdWorkLocation business object, Wave 4 deferred bucket). Ports the essential active validation
/// only: required fields, EndDate &gt;= StartDate, and no overlapping StartDate/EndDate window for the same
/// Employee+WorkLocation. The legacy cross-check against <c>EmployeeOrgAssignment</c>'s default WorkLocation
/// is NOT ported (cross-entity, application-layer concern only if needed).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations)]
public class EmployeeExtdWorkLocationsAppService : HCMv3AppServiceBase, IEmployeeExtdWorkLocationsAppService
{
    private readonly IRepository<EmployeeExtdWorkLocation> _repository;

    public EmployeeExtdWorkLocationsAppService(IRepository<EmployeeExtdWorkLocation> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeExtdWorkLocationDto>> GetAll(GetAllEmployeeExtdWorkLocationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WorkLocation)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeExtdWorkLocationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeExtdWorkLocationForEditOutput> GetEmployeeExtdWorkLocationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeExtdWorkLocationForEditOutput
        {
            EmployeeExtdWorkLocation = new CreateOrEditEmployeeExtdWorkLocationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WorkLocationId = entity.WorkLocationId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeExtdWorkLocationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeExtdWorkLocationDto input)
    {
        var entity = MapToEntity(input, new EmployeeExtdWorkLocation());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeExtdWorkLocationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmployeeExtdWorkLocation entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var hasOverlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.WorkLocationId == entity.WorkLocationId
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date);
        if (hasOverlap)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmployeeExtdWorkLocation MapToEntity(CreateOrEditEmployeeExtdWorkLocationDto input, EmployeeExtdWorkLocation entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WorkLocationId = input.WorkLocationId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        return entity;
    }

    private static EmployeeExtdWorkLocationDto ToDto(EmployeeExtdWorkLocation entity)
    {
        return new EmployeeExtdWorkLocationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationNameEn = entity.WorkLocation?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
