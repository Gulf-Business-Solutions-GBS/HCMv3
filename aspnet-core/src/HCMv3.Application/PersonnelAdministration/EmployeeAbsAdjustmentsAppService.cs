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
/// Application service for <see cref="EmployeeAbsAdjustment"/> (ported from legacy XAF
/// EmployeeAbsAdjustment business object, Wave 1 deferred bucket). Records the manual adjustment
/// transaction itself; the legacy cascade that adds/subtracts <c>QuotaAdjusted</c> on the linked
/// <see cref="EmployeeAbsenceQuota"/> record (with negative-balance validation) is NOT ported here -- see
/// class remarks on <see cref="EmployeeAbsAdjustment"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments)]
public class EmployeeAbsAdjustmentsAppService : HCMv3AppServiceBase, IEmployeeAbsAdjustmentsAppService
{
    private readonly IRepository<EmployeeAbsAdjustment> _repository;

    public EmployeeAbsAdjustmentsAppService(IRepository<EmployeeAbsAdjustment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsAdjustmentDto>> GetAll(GetAllEmployeeAbsAdjustmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsAdjustmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsAdjustmentForEditOutput> GetEmployeeAbsAdjustmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsAdjustmentForEditOutput
        {
            EmployeeAbsAdjustment = new CreateOrEditEmployeeAbsAdjustmentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                AdjustmentType = entity.AdjustmentType,
                EffectiveDate = entity.EffectiveDate,
                QuotaAdjusted = entity.QuotaAdjusted,
                Comments = entity.Comments,
                AdjustmentPaymentType = entity.AdjustmentPaymentType
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsAdjustmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsAdjustmentDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsAdjustment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsAdjustmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeAbsAdjustment MapToEntity(CreateOrEditEmployeeAbsAdjustmentDto input, EmployeeAbsAdjustment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.AdjustmentType = input.AdjustmentType;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 6);
        entity.Comments = input.Comments;
        entity.AdjustmentPaymentType = input.AdjustmentPaymentType;
        return entity;
    }

    private static EmployeeAbsAdjustmentDto ToDto(EmployeeAbsAdjustment entity)
    {
        return new EmployeeAbsAdjustmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            AdjustmentType = entity.AdjustmentType,
            EffectiveDate = entity.EffectiveDate,
            QuotaAdjusted = entity.QuotaAdjusted,
            Comments = entity.Comments,
            AdjustmentPaymentType = entity.AdjustmentPaymentType
        };
    }
}
