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
/// Application service for the monthly gratuity accrual snapshot per employee (ported from legacy XAF
/// GratuityAccrualSummary business object). No business validation is ported — see
/// <see cref="GratuityAccrualSummary"/> remarks (the legacy's swallowed-empty-catch anti-pattern is not
/// replicated).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries)]
public class GratuityAccrualSummariesAppService : HCMv3AppServiceBase, IGratuityAccrualSummariesAppService
{
    private readonly IRepository<GratuityAccrualSummary> _gratuityAccrualSummaryRepository;

    public GratuityAccrualSummariesAppService(IRepository<GratuityAccrualSummary> gratuityAccrualSummaryRepository)
    {
        _gratuityAccrualSummaryRepository = gratuityAccrualSummaryRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<GratuityAccrualSummaryDto>> GetAll(GetAllGratuityAccrualSummariesInput input)
    {
        input.Normalize();

        var query = _gratuityAccrualSummaryRepository.GetAll()
            .Include(s => s.Employee)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<GratuityAccrualSummaryDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetGratuityAccrualSummaryForEditOutput> GetGratuityAccrualSummaryForEdit(EntityDto input)
    {
        var entity = await _gratuityAccrualSummaryRepository.GetAsync(input.Id);

        return new GetGratuityAccrualSummaryForEditOutput
        {
            GratuityAccrualSummary = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditGratuityAccrualSummaryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _gratuityAccrualSummaryRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditGratuityAccrualSummaryDto input)
    {
        var entity = MapToEntity(input, new GratuityAccrualSummary());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _gratuityAccrualSummaryRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditGratuityAccrualSummaryDto input)
    {
        var entity = await _gratuityAccrualSummaryRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _gratuityAccrualSummaryRepository.UpdateAsync(entity);
    }

    private static GratuityAccrualSummary MapToEntity(CreateOrEditGratuityAccrualSummaryDto input, GratuityAccrualSummary entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Year = input.Year;
        entity.Month = input.Month;
        entity.GratuityDaysAccrued = input.GratuityDaysAccrued;
        entity.GratuityDaysBalance = input.GratuityDaysBalance;
        entity.GratuityAmount = input.GratuityAmount;
        entity.GratuityAmountPaid = input.GratuityAmountPaid;

        return entity;
    }

    private static GratuityAccrualSummaryDto ToDto(GratuityAccrualSummary entity)
    {
        return new GratuityAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            Year = entity.Year,
            Month = entity.Month,
            GratuityDaysAccrued = entity.GratuityDaysAccrued,
            GratuityDaysBalance = entity.GratuityDaysBalance,
            GratuityAmount = entity.GratuityAmount,
            GratuityAmountPaid = entity.GratuityAmountPaid
        };
    }

    private static CreateOrEditGratuityAccrualSummaryDto ToCreateOrEditDto(GratuityAccrualSummary entity)
    {
        return new CreateOrEditGratuityAccrualSummaryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Year = entity.Year,
            Month = entity.Month,
            GratuityDaysAccrued = entity.GratuityDaysAccrued,
            GratuityDaysBalance = entity.GratuityDaysBalance,
            GratuityAmount = entity.GratuityAmount,
            GratuityAmountPaid = entity.GratuityAmountPaid
        };
    }
}
