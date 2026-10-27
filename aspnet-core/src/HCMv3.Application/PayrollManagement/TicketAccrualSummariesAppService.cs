using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the monthly air-ticket accrual snapshot per employee (ported from legacy XAF
/// TicketAccrualSummary business object). No business validation is ported — see
/// <see cref="TicketAccrualSummary"/> remarks (the legacy's swallowed-empty-catch anti-pattern is not
/// replicated).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries)]
public class TicketAccrualSummariesAppService : HCMv3AppServiceBase, ITicketAccrualSummariesAppService
{
    private readonly IRepository<TicketAccrualSummary> _ticketAccrualSummaryRepository;

    public TicketAccrualSummariesAppService(IRepository<TicketAccrualSummary> ticketAccrualSummaryRepository)
    {
        _ticketAccrualSummaryRepository = ticketAccrualSummaryRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TicketAccrualSummaryDto>> GetAll(GetAllTicketAccrualSummariesInput input)
    {
        input.Normalize();

        var query = _ticketAccrualSummaryRepository.GetAll()
            .Include(s => s.Employee)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TicketAccrualSummaryDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTicketAccrualSummaryForEditOutput> GetTicketAccrualSummaryForEdit(EntityDto input)
    {
        var entity = await _ticketAccrualSummaryRepository.GetAsync(input.Id);

        return new GetTicketAccrualSummaryForEditOutput
        {
            TicketAccrualSummary = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTicketAccrualSummaryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _ticketAccrualSummaryRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTicketAccrualSummaryDto input)
    {
        var entity = MapToEntity(input, new TicketAccrualSummary());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _ticketAccrualSummaryRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTicketAccrualSummaryDto input)
    {
        var entity = await _ticketAccrualSummaryRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _ticketAccrualSummaryRepository.UpdateAsync(entity);
    }

    private static TicketAccrualSummary MapToEntity(CreateOrEditTicketAccrualSummaryDto input, TicketAccrualSummary entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Year = input.Year;
        entity.Month = input.Month;
        entity.TicketAccrued = input.TicketAccrued;
        entity.TicketUsed = input.TicketUsed;

        return entity;
    }

    private static TicketAccrualSummaryDto ToDto(TicketAccrualSummary entity)
    {
        return new TicketAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            Year = entity.Year,
            Month = entity.Month,
            TicketAccrued = entity.TicketAccrued,
            TicketUsed = entity.TicketUsed
        };
    }

    private static CreateOrEditTicketAccrualSummaryDto ToCreateOrEditDto(TicketAccrualSummary entity)
    {
        return new CreateOrEditTicketAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Year = entity.Year,
            Month = entity.Month,
            TicketAccrued = entity.TicketAccrued,
            TicketUsed = entity.TicketUsed
        };
    }
}
