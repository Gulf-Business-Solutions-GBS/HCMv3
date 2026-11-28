using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeTktAdjustment"/> (legacy XAF EmployeeTktAdjustment, menu
/// "Ticket Adjustment"). Saving and deleting move the adjusted count on the employee's
/// <see cref="EmployeeTicketQuota"/> whose entitlement period covers the effective date, exactly as legacy's
/// OnSaving / OnDeleting did -- see <see cref="IEmployeeTktAdjustmentQuotaService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments)]
public class EmployeeTktAdjustmentsAppService : HCMv3AppServiceBase, IEmployeeTktAdjustmentsAppService
{
    private readonly IRepository<EmployeeTktAdjustment> _repository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeTktAdjustmentQuotaService _quotaService;

    public EmployeeTktAdjustmentsAppService(
        IRepository<EmployeeTktAdjustment> repository,
        IRepository<Employee> employeeRepository,
        IEmployeeTktAdjustmentQuotaService quotaService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _quotaService = quotaService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTktAdjustmentDto>> GetAll(GetAllEmployeeTktAdjustmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.TicketType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Employee.Code.Contains(input.Filter) ||
                a.Employee.FullNameEn.Contains(input.Filter) ||
                a.Employee.FullNameAr.Contains(input.Filter) ||
                a.TicketType.NameEn.Contains(input.Filter) ||
                a.TicketType.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTktAdjustmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTktAdjustmentForEditOutput> GetEmployeeTktAdjustmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAll()
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == input.Id)
            ?? await _repository.GetAsync(input.Id);

        return new GetEmployeeTktAdjustmentForEditOutput
        {
            EmployeeTktAdjustment = ToCreateOrEditDto(entity),
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn
        };
    }

    /// <summary>
    /// Every employee, for the form's dropdown when it is opened from the Employee Ticket Adjustments list
    /// (legacy menu "Ticket Adjustment"). Under this screen's own permission.
    /// </summary>
    [HttpPost]
    public async Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeTktAdjustmentEmployeesInput input)
    {
        var filter = input.Filter?.Trim();

        var items = await _employeeRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(filter) ||
                e.FullNameEn.Contains(filter) ||
                e.FullNameAr.Contains(filter))
            .OrderBy(e => e.Code.Length)
            .ThenBy(e => e.Code)
            .Select(e => new SimpleLookupDto
            {
                Id = e.Id,
                Code = e.Code,
                NameEn = e.FullNameEn,
                NameAr = e.FullNameAr
            })
            .ToListAsync();

        return new ListResultDto<SimpleLookupDto>(items);
    }

    /// <summary>
    /// The quota the adjustment will draw on and how much of it is left, so the form can show it and fill
    /// Quota Accrued while the user types. Saving re-checks all of it.
    /// </summary>
    [HttpPost]
    public async Task<EmployeeTktAdjustmentQuotaDto> GetQuota(GetEmployeeTktAdjustmentQuotaInput input)
    {
        var quota = await _quotaService.FindQuotaAsync(input.EmployeeId, input.TicketTypeId, input.EffectiveDate);
        if (quota == null)
        {
            return new EmployeeTktAdjustmentQuotaDto { QuotaFound = false };
        }

        var balance = EmployeeTktAdjustmentQuotaService.Balance(quota);
        var available = balance;

        if (input.Id.HasValue)
        {
            var saved = await _repository.FirstOrDefaultAsync(input.Id.Value);
            if (saved != null)
            {
                var savedQuota = await _quotaService.FindQuotaAsync(saved.EmployeeId, saved.TicketTypeId, saved.EffectiveDate);
                if (savedQuota != null && savedQuota.Id == quota.Id)
                {
                    available = System.Math.Round(balance + saved.QuotaAdjusted, 3);
                }
            }
        }

        return new EmployeeTktAdjustmentQuotaDto
        {
            QuotaFound = true,
            EntitlementFrom = quota.EntitlementFrom,
            EntitlementTo = quota.EntitlementTo,
            QuotaAccrued = quota.QuotaAccrued,
            QuotaUsed = quota.QuotaUsed,
            QuotaAdjusted = quota.QuotaAdjusted,
            QuotaBalance = balance,
            AvailableBalance = available
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTktAdjustmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        // Legacy OnDeleting: the adjusted count goes back to the quota.
        await _quotaService.ReverseAsync(TktAdjustmentQuotaSnapshot.Of(entity));

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTktAdjustmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeTktAdjustment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Legacy OnSaving: take the adjusted count from the quota (refused beyond its balance).
        await _quotaService.ApplyAsync(null, entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTktAdjustmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previous = TktAdjustmentQuotaSnapshot.Of(entity);

        MapToEntity(input, entity);

        // Legacy OnSaving: old value back to its quota, new value taken from the quota now covering the date.
        await _quotaService.ApplyAsync(previous, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeeTktAdjustment MapToEntity(CreateOrEditEmployeeTktAdjustmentDto input, EmployeeTktAdjustment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TicketTypeId = input.TicketTypeId;
        entity.AdjustmentType = input.AdjustmentType;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.QuotaAccrued = System.Math.Round(input.QuotaAccrued, 6);
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 6);
        entity.TicketPaymentType = input.TicketPaymentType;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeTktAdjustmentDto ToDto(EmployeeTktAdjustment entity)
    {
        return new EmployeeTktAdjustmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TicketTypeId = entity.TicketTypeId,
            TicketTypeName = entity.TicketType?.NameEn,
            AdjustmentType = entity.AdjustmentType,
            EffectiveDate = entity.EffectiveDate,
            QuotaAccrued = entity.QuotaAccrued,
            QuotaAdjusted = entity.QuotaAdjusted,
            TicketPaymentType = entity.TicketPaymentType,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditEmployeeTktAdjustmentDto ToCreateOrEditDto(EmployeeTktAdjustment entity)
    {
        return new CreateOrEditEmployeeTktAdjustmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TicketTypeId = entity.TicketTypeId,
            AdjustmentType = entity.AdjustmentType,
            EffectiveDate = entity.EffectiveDate,
            QuotaAccrued = entity.QuotaAccrued,
            QuotaAdjusted = entity.QuotaAdjusted,
            TicketPaymentType = entity.TicketPaymentType,
            Comments = entity.Comments
        };
    }
}
