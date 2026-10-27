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
/// Application service for the fixed pay-component wage type entry of an <see cref="EmployeeSubGroup"/>
/// (ported from legacy XAF EmployeeSubGroupPayment business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupPayment.EmployeeSubGroupId"/>. <c>WageTypeId</c> is a nullable FK to the
/// not-yet-ported <c>WageType</c> lookup (a later wave). DELIBERATE SIMPLIFICATION: the legacy duplicate
/// WageType check and the cross-field rule validating
/// <c>WageType.IncludeInCalculatedPay</c>/<c>IncludeInBasicPay</c> against <see cref="PayInterval"/> are not
/// ported -- WageType does not exist yet this wave, so only basic required-field validation applies (per the
/// design brief for this entity).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments)]
public class EmployeeSubGroupPaymentsAppService : HCMv3AppServiceBase, IEmployeeSubGroupPaymentsAppService
{
    private readonly IRepository<EmployeeSubGroupPayment> _employeeSubGroupPaymentRepository;

    public EmployeeSubGroupPaymentsAppService(IRepository<EmployeeSubGroupPayment> employeeSubGroupPaymentRepository)
    {
        _employeeSubGroupPaymentRepository = employeeSubGroupPaymentRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupPaymentDto>> GetAll(GetAllEmployeeSubGroupPaymentsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupPaymentRepository.GetAll()
            .Include(p => p.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, p => p.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupPaymentDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupPaymentForEditOutput> GetEmployeeSubGroupPaymentForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupPaymentRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupPaymentForEditOutput
        {
            EmployeeSubGroupPayment = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupPaymentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupPaymentRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupPaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupPayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _employeeSubGroupPaymentRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupPaymentDto input)
    {
        var entity = await _employeeSubGroupPaymentRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _employeeSubGroupPaymentRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupPayment MapToEntity(CreateOrEditEmployeeSubGroupPaymentDto input, EmployeeSubGroupPayment entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.PayInterval = input.PayInterval;
        entity.PayMonth = input.PayMonth;

        return entity;
    }

    private static EmployeeSubGroupPaymentDto ToDto(EmployeeSubGroupPayment entity)
    {
        return new EmployeeSubGroupPaymentDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            WageTypeId = entity.WageTypeId,
            Amount = entity.Amount,
            PayInterval = entity.PayInterval,
            PayMonth = entity.PayMonth
        };
    }

    private static CreateOrEditEmployeeSubGroupPaymentDto ToCreateOrEditDto(EmployeeSubGroupPayment entity)
    {
        return new CreateOrEditEmployeeSubGroupPaymentDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            WageTypeId = entity.WageTypeId,
            Amount = entity.Amount,
            PayInterval = entity.PayInterval,
            PayMonth = entity.PayMonth
        };
    }
}
