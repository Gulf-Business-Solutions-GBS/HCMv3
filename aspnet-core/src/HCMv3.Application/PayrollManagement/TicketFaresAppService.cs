using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="TicketFare"/> (effective-dated rate matrix cell for (TicketRoute,
/// TicketPaxType, TicketClass)). Ported from legacy XAF TicketFare business object. Ports the legacy
/// EndDate auto-chaining (<c>SetEndDate</c>), mirroring <c>OrgUnitWageAccGroupsAppService</c>'s pattern,
/// scoped by (TicketRouteId, TicketPaxTypeId, TicketClassId) instead of OrganizationUnitId.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketFares)]
public class TicketFaresAppService : HCMv3AppServiceBase, ITicketFaresAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<TicketFare> _repository;

    public TicketFaresAppService(IRepository<TicketFare> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TicketFareDto>> GetAll(GetAllTicketFaresInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.TicketRoute)
            .Include(f => f.TicketPaxType)
            .Include(f => f.TicketClass)
            .WhereIf(input.TicketRouteId.HasValue, f => f.TicketRouteId == input.TicketRouteId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TicketFareDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTicketFareForEditOutput> GetTicketFareForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTicketFareForEditOutput
        {
            TicketFare = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTicketFareDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketFares_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTicketFareDto input)
    {
        var entity = new TicketFare
        {
            TenantId = AbpSession.TenantId ?? 0,
            TicketRouteId = input.TicketRouteId,
            TicketPaxTypeId = input.TicketPaxTypeId,
            TicketClassId = input.TicketClassId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate,
            OneWayTicketFare = input.OneWayTicketFare,
            ReturnTicketFare = input.ReturnTicketFare
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.TicketRouteId, entity.TicketPaxTypeId, entity.TicketClassId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketFares_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTicketFareDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.TicketRouteId = input.TicketRouteId;
        entity.TicketPaxTypeId = input.TicketPaxTypeId;
        entity.TicketClassId = input.TicketClassId;
        entity.StartDate = input.StartDate.Date;
        entity.OneWayTicketFare = input.OneWayTicketFare;
        entity.ReturnTicketFare = input.ReturnTicketFare;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.TicketRouteId, entity.TicketPaxTypeId, entity.TicketClassId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketFares_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var ticketRouteId = entity.TicketRouteId;
        var ticketPaxTypeId = entity.TicketPaxTypeId;
        var ticketClassId = entity.TicketClassId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(ticketRouteId, ticketPaxTypeId, ticketClassId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required FKs (enforced via DTO <c>[Required]</c>),
    /// non-negative fares, no duplicate (TicketRouteId, TicketClassId, TicketPaxTypeId, StartDate).
    /// </summary>
    private async Task ValidateAsync(TicketFare entity)
    {
        if (entity.OneWayTicketFare < 0 || entity.ReturnTicketFare < 0)
        {
            throw new UserFriendlyException(L("TicketFareCannotBeNegative"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           f.TicketRouteId == entity.TicketRouteId &&
                           f.TicketClassId == entity.TicketClassId &&
                           f.TicketPaxTypeId == entity.TicketPaxTypeId &&
                           f.StartDate == entity.StartDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the (TicketRoute, TicketPaxType,
    /// TicketClass) scope (ordered by StartDate), EndDate = (next record's StartDate - 1 day), or
    /// infinite for the last one.
    /// </summary>
    /// <summary>
    /// Property-injected: this service is built by the container, and a chained row must be marked before it is
    /// saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

    private async Task SetEndDatesAsync(int ticketRouteId, int ticketPaxTypeId, int ticketClassId)
    {
        var records = await _repository.GetAll()
            .Where(f => f.TicketRouteId == ticketRouteId &&
                        f.TicketPaxTypeId == ticketPaxTypeId &&
                        f.TicketClassId == ticketClassId)
            .OrderBy(f => f.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var next = records.Skip(i + 1).FirstOrDefault();
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (records[i].EndDate != newEndDate)
            {
                records[i].EndDate = newEndDate;
                
                // Re-closed because a neighbour moved, not a change of its own: it must not move the employee's
                // EarliestMasterDataChange. Legacy sets IsEndDateAutoChange on exactly these rows.
                RetroTrigger?.MarkEndDateAutoChange(records[i]);
            }
        }
    }

    private static TicketFareDto ToDto(TicketFare entity)
    {
        return new TicketFareDto
        {
            Id = entity.Id,
            TicketRouteId = entity.TicketRouteId,
            TicketRouteName = entity.TicketRoute?.NameEn,
            TicketPaxTypeId = entity.TicketPaxTypeId,
            TicketPaxTypeName = entity.TicketPaxType?.NameEn,
            TicketClassId = entity.TicketClassId,
            TicketClassName = entity.TicketClass?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OneWayTicketFare = entity.OneWayTicketFare,
            ReturnTicketFare = entity.ReturnTicketFare
        };
    }

    private static CreateOrEditTicketFareDto ToCreateOrEditDto(TicketFare entity)
    {
        return new CreateOrEditTicketFareDto
        {
            Id = entity.Id,
            TicketRouteId = entity.TicketRouteId,
            TicketPaxTypeId = entity.TicketPaxTypeId,
            TicketClassId = entity.TicketClassId,
            StartDate = entity.StartDate,
            OneWayTicketFare = entity.OneWayTicketFare,
            ReturnTicketFare = entity.ReturnTicketFare
        };
    }
}
