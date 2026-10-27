using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the date-range air-ticket entitlement of an <see cref="EmployeeSubGroup"/>
/// (ported from legacy XAF EmployeeSubGroupTicket business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupTicket.EmployeeSubGroupId"/>. <c>TicketTypeId</c> is a nullable FK to the
/// not-yet-ported <c>TicketType</c> lookup (a later wave).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets)]
public class EmployeeSubGroupTicketsAppService : HCMv3AppServiceBase, IEmployeeSubGroupTicketsAppService
{
    private readonly IRepository<EmployeeSubGroupTicket> _employeeSubGroupTicketRepository;

    public EmployeeSubGroupTicketsAppService(IRepository<EmployeeSubGroupTicket> employeeSubGroupTicketRepository)
    {
        _employeeSubGroupTicketRepository = employeeSubGroupTicketRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupTicketDto>> GetAll(GetAllEmployeeSubGroupTicketsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupTicketRepository.GetAll()
            .Include(t => t.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, t => t.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupTicketDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupTicketForEditOutput> GetEmployeeSubGroupTicketForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupTicketRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupTicketForEditOutput
        {
            EmployeeSubGroupTicket = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupTicketDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupTicketRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupTicketDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupTicket());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupTicketRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupTicketDto input)
    {
        var entity = await _employeeSubGroupTicketRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupTicketRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupTicket MapToEntity(CreateOrEditEmployeeSubGroupTicketDto input, EmployeeSubGroupTicket entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.TicketTypeId = input.TicketTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.TicketCount = input.TicketCount;
        entity.MaxAmount = input.MaxAmount;
        entity.FixAmount = input.FixAmount;
        entity.FareType = input.FareType;
        entity.AgeLimit = input.AgeLimit;
        entity.AccrualPeriod = input.AccrualPeriod;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate and no overlapping date range with a
    /// sibling row for the same (EmployeeSubGroupId, TicketTypeId) pair (the legacy source ran this overlap
    /// check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupTicket entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _employeeSubGroupTicketRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           t.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           t.TicketTypeId == entity.TicketTypeId &&
                           t.StartDate.Date <= entity.EndDate.Date &&
                           t.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static EmployeeSubGroupTicketDto ToDto(EmployeeSubGroupTicket entity)
    {
        return new EmployeeSubGroupTicketDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            TicketTypeId = entity.TicketTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TicketCount = entity.TicketCount,
            MaxAmount = entity.MaxAmount,
            FixAmount = entity.FixAmount,
            FareType = entity.FareType,
            AgeLimit = entity.AgeLimit,
            AccrualPeriod = entity.AccrualPeriod
        };
    }

    private static CreateOrEditEmployeeSubGroupTicketDto ToCreateOrEditDto(EmployeeSubGroupTicket entity)
    {
        return new CreateOrEditEmployeeSubGroupTicketDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            TicketTypeId = entity.TicketTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TicketCount = entity.TicketCount,
            MaxAmount = entity.MaxAmount,
            FixAmount = entity.FixAmount,
            FareType = entity.FareType,
            AgeLimit = entity.AgeLimit,
            AccrualPeriod = entity.AccrualPeriod
        };
    }
}
