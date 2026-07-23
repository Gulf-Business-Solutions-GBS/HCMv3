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
/// Application service for the monthly leave accrual snapshot per employee/leave-type (ported from legacy
/// XAF LeaveAccrualSummary business object). No business validation is ported — see
/// <see cref="LeaveAccrualSummary"/> remarks (the legacy's swallowed-empty-catch anti-pattern is not
/// replicated).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries)]
public class LeaveAccrualSummariesAppService : HCMv3AppServiceBase, ILeaveAccrualSummariesAppService
{
    private readonly IRepository<LeaveAccrualSummary> _leaveAccrualSummaryRepository;

    public LeaveAccrualSummariesAppService(IRepository<LeaveAccrualSummary> leaveAccrualSummaryRepository)
    {
        _leaveAccrualSummaryRepository = leaveAccrualSummaryRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<LeaveAccrualSummaryDto>> GetAll(GetAllLeaveAccrualSummariesInput input)
    {
        input.Normalize();

        var query = _leaveAccrualSummaryRepository.GetAll()
            .Include(s => s.Employee)
            .Include(s => s.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<LeaveAccrualSummaryDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetLeaveAccrualSummaryForEditOutput> GetLeaveAccrualSummaryForEdit(EntityDto input)
    {
        var entity = await _leaveAccrualSummaryRepository.GetAsync(input.Id);

        return new GetLeaveAccrualSummaryForEditOutput
        {
            LeaveAccrualSummary = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditLeaveAccrualSummaryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _leaveAccrualSummaryRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditLeaveAccrualSummaryDto input)
    {
        var entity = MapToEntity(input, new LeaveAccrualSummary());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _leaveAccrualSummaryRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditLeaveAccrualSummaryDto input)
    {
        var entity = await _leaveAccrualSummaryRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _leaveAccrualSummaryRepository.UpdateAsync(entity);
    }

    private static LeaveAccrualSummary MapToEntity(CreateOrEditLeaveAccrualSummaryDto input, LeaveAccrualSummary entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.Year = input.Year;
        entity.Month = input.Month;
        entity.AccrualDays = input.AccrualDays;
        entity.UsedDays = input.UsedDays;
        entity.CumulativeAccruedDays = input.CumulativeAccruedDays;

        return entity;
    }

    private static LeaveAccrualSummaryDto ToDto(LeaveAccrualSummary entity)
    {
        return new LeaveAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            Year = entity.Year,
            Month = entity.Month,
            AccrualDays = entity.AccrualDays,
            UsedDays = entity.UsedDays,
            CumulativeAccruedDays = entity.CumulativeAccruedDays
        };
    }

    private static CreateOrEditLeaveAccrualSummaryDto ToCreateOrEditDto(LeaveAccrualSummary entity)
    {
        return new CreateOrEditLeaveAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            AbsenceTypeId = entity.AbsenceTypeId,
            Year = entity.Year,
            Month = entity.Month,
            AccrualDays = entity.AccrualDays,
            UsedDays = entity.UsedDays,
            CumulativeAccruedDays = entity.CumulativeAccruedDays
        };
    }
}
