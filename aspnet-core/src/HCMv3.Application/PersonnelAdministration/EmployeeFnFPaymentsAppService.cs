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
/// Application service for <see cref="EmployeeFnFPayment"/> (ported from legacy XAF EmployeeFnFPayment
/// business object, Wave 3 deferred bucket). Only plain CRUD over the persisted ledger/compensation columns
/// is ported -- the legacy's extensive cross-module orchestration (leave/indemnity/ticket adjustment
/// auto-generation, loan settlement, position-status update, salary-register/payroll-lock validation, user
/// deactivation) is NOT ported -- see <see cref="EmployeeFnFPayment"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments)]
public class EmployeeFnFPaymentsAppService : HCMv3AppServiceBase, IEmployeeFnFPaymentsAppService
{
    private readonly IRepository<EmployeeFnFPayment> _repository;

    public EmployeeFnFPaymentsAppService(IRepository<EmployeeFnFPayment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeFnFPaymentDto>> GetAll(GetAllEmployeeFnFPaymentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeFnFPaymentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeFnFPaymentForEditOutput> GetEmployeeFnFPaymentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeFnFPaymentForEditOutput
        {
            EmployeeFnFPayment = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeFnFPaymentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeFnFPaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeeFnFPayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFnFPaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeeFnFPayment MapToEntity(CreateOrEditEmployeeFnFPaymentDto input, EmployeeFnFPayment entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EmployeeActionId = input.EmployeeActionId;
        entity.EmployeeBasicPayDetailId = input.EmployeeBasicPayDetailId;
        entity.CompensationDate = input.CompensationDate.Date;
        entity.LeaveCompensateDays = input.LeaveCompensateDays;
        entity.GratuityCompensateDays = input.GratuityCompensateDays;
        entity.IndemnityCompensateAmount = input.IndemnityCompensateAmount;
        entity.IndemnityPayAdjustment = input.IndemnityPayAdjustment;
        entity.TicketCompensateCount = input.TicketCompensateCount;
        entity.Dependant1TicketCompensateCount = input.Dependant1TicketCompensateCount;
        entity.Dependant2TicketCompensateCount = input.Dependant2TicketCompensateCount;
        entity.Dependant3TicketCompensateCount = input.Dependant3TicketCompensateCount;
        entity.CompensateLoanAmount = input.CompensateLoanAmount;
        entity.CompensateBonusPay = input.CompensateBonusPay;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeFnFPaymentDto ToDto(EmployeeFnFPayment entity)
    {
        return new EmployeeFnFPaymentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            EmployeeActionId = entity.EmployeeActionId,
            EmployeeBasicPayDetailId = entity.EmployeeBasicPayDetailId,
            CompensationDate = entity.CompensationDate,
            LeaveCompensateDays = entity.LeaveCompensateDays,
            GratuityCompensateDays = entity.GratuityCompensateDays,
            IndemnityCompensateAmount = entity.IndemnityCompensateAmount,
            IndemnityPayAdjustment = entity.IndemnityPayAdjustment,
            TicketCompensateCount = entity.TicketCompensateCount,
            Dependant1TicketCompensateCount = entity.Dependant1TicketCompensateCount,
            Dependant2TicketCompensateCount = entity.Dependant2TicketCompensateCount,
            Dependant3TicketCompensateCount = entity.Dependant3TicketCompensateCount,
            CompensateLoanAmount = entity.CompensateLoanAmount,
            CompensateBonusPay = entity.CompensateBonusPay,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditEmployeeFnFPaymentDto ToCreateOrEditDto(EmployeeFnFPayment entity)
    {
        return new CreateOrEditEmployeeFnFPaymentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeActionId = entity.EmployeeActionId,
            EmployeeBasicPayDetailId = entity.EmployeeBasicPayDetailId,
            CompensationDate = entity.CompensationDate,
            LeaveCompensateDays = entity.LeaveCompensateDays,
            GratuityCompensateDays = entity.GratuityCompensateDays,
            IndemnityCompensateAmount = entity.IndemnityCompensateAmount,
            IndemnityPayAdjustment = entity.IndemnityPayAdjustment,
            TicketCompensateCount = entity.TicketCompensateCount,
            Dependant1TicketCompensateCount = entity.Dependant1TicketCompensateCount,
            Dependant2TicketCompensateCount = entity.Dependant2TicketCompensateCount,
            Dependant3TicketCompensateCount = entity.Dependant3TicketCompensateCount,
            CompensateLoanAmount = entity.CompensateLoanAmount,
            CompensateBonusPay = entity.CompensateBonusPay,
            Comments = entity.Comments
        };
    }
}
