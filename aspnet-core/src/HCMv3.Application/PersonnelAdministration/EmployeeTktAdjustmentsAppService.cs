using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeTktAdjustment"/> (ported from legacy XAF
/// EmployeeTktAdjustment business object, Wave 3 deferred bucket). Records the manual adjustment
/// transaction itself; the legacy cascade that adds/subtracts <c>QuotaAdjusted</c> on the linked
/// <see cref="EmployeeTicketQuota"/> record (with negative-balance validation) is NOT ported here -- same
/// approach as Wave 1's <c>EmployeeAbsAdjustment</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments)]
public class EmployeeTktAdjustmentsAppService : HCMv3AppServiceBase, IEmployeeTktAdjustmentsAppService
{
    private readonly IRepository<EmployeeTktAdjustment> _repository;

    public EmployeeTktAdjustmentsAppService(IRepository<EmployeeTktAdjustment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTktAdjustmentDto>> GetAll(GetAllEmployeeTktAdjustmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.TicketType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTktAdjustmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTktAdjustmentForEditOutput> GetEmployeeTktAdjustmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTktAdjustmentForEditOutput
        {
            EmployeeTktAdjustment = ToCreateOrEditDto(entity)
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
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTktAdjustmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeTktAdjustment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTktAdjustmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

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
