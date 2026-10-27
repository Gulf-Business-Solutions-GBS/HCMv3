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
/// Application service for <see cref="EmployeePayment"/> (ported from legacy XAF EmployeePayment business
/// object, Wave 4 deferred bucket). Ports the essential required-field validation only. The legacy
/// <c>ExpectedAmount</c> wage-rate computation is NOT ported -- see class remarks on
/// <see cref="EmployeePayment"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments)]
public class EmployeePaymentsAppService : HCMv3AppServiceBase, IEmployeePaymentsAppService
{
    private readonly IRepository<EmployeePayment> _repository;

    public EmployeePaymentsAppService(IRepository<EmployeePayment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeePaymentDto>> GetAll(GetAllEmployeePaymentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .Include(e => e.Currency)
            .Include(e => e.CostCenter)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeePaymentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeePaymentForEditOutput> GetEmployeePaymentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeePaymentForEditOutput
        {
            EmployeePayment = new CreateOrEditEmployeePaymentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                CurrencyId = entity.CurrencyId,
                PaymentDate = entity.PaymentDate,
                Comments = entity.Comments,
                UnitType = entity.UnitType,
                CostCenterId = entity.CostCenterId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeePaymentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeePaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeePayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeePaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeePayment MapToEntity(CreateOrEditEmployeePaymentDto input, EmployeePayment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.Comments = input.Comments;
        entity.UnitType = input.UnitType;
        entity.CostCenterId = input.CostCenterId;
        return entity;
    }

    private static EmployeePaymentDto ToDto(EmployeePayment entity)
    {
        return new EmployeePaymentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyNameEn = entity.Currency?.NameEn,
            PaymentDate = entity.PaymentDate,
            Comments = entity.Comments,
            UnitType = entity.UnitType,
            CostCenterId = entity.CostCenterId,
            CostCenterNameEn = entity.CostCenter?.NameEn
        };
    }
}
