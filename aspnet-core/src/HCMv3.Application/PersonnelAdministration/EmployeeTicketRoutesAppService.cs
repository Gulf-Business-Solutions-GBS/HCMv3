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
/// Application service for <see cref="EmployeeTicketRoute"/> (ported from legacy XAF EmployeeTicketRoute
/// business object, Wave 3 deferred bucket). Ports the legacy overlap validation: no two
/// EmployeeTicketRoute rows for the same employee with overlapping Start/End date ranges.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes)]
public class EmployeeTicketRoutesAppService : HCMv3AppServiceBase, IEmployeeTicketRoutesAppService
{
    private readonly IRepository<EmployeeTicketRoute> _repository;

    public EmployeeTicketRoutesAppService(IRepository<EmployeeTicketRoute> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTicketRouteDto>> GetAll(GetAllEmployeeTicketRoutesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.TicketRoute)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTicketRouteDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTicketRouteForEditOutput> GetEmployeeTicketRouteForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTicketRouteForEditOutput
        {
            EmployeeTicketRoute = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTicketRouteDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTicketRouteDto input)
    {
        var entity = MapToEntity(input, new EmployeeTicketRoute());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTicketRouteDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no overlapping date ranges for the same employee.
    /// </summary>
    private async Task ValidateAsync(EmployeeTicketRoute entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.EmployeeId == entity.EmployeeId &&
                           r.StartDate <= entity.EndDate &&
                           r.EndDate >= entity.StartDate);
        if (overlap)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeTicketRoute MapToEntity(CreateOrEditEmployeeTicketRouteDto input, EmployeeTicketRoute entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TicketRouteId = input.TicketRouteId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        return entity;
    }

    private static EmployeeTicketRouteDto ToDto(EmployeeTicketRoute entity)
    {
        return new EmployeeTicketRouteDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TicketRouteId = entity.TicketRouteId,
            TicketRouteName = entity.TicketRoute?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditEmployeeTicketRouteDto ToCreateOrEditDto(EmployeeTicketRoute entity)
    {
        return new CreateOrEditEmployeeTicketRouteDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TicketRouteId = entity.TicketRouteId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
