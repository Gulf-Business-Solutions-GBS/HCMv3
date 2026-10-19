using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeOnHold"/> (ported from legacy XAF EmployeeOnHold business
/// object, Wave 2 deferred bucket). Ports the essential active validations only (required fields, EndDate
/// not earlier than StartDate, no-overlapping-hold-period for the same employee). The legacy
/// PayrollControlRecord lock-date check and StartDate/EndDate month-normalization are NOT ported -- see
/// class remarks on <see cref="EmployeeOnHold"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds)]
public class EmployeeOnHoldsAppService : HCMv3AppServiceBase, IEmployeeOnHoldsAppService
{
    private readonly IRepository<EmployeeOnHold> _repository;
    private readonly IEmployeeOnHoldService _onHoldService;

    public EmployeeOnHoldsAppService(IRepository<EmployeeOnHold> repository, IEmployeeOnHoldService onHoldService)
    {
        _repository = repository;
        _onHoldService = onHoldService;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeOnHoldDto>> GetAll(GetAllEmployeeOnHoldsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(o => o.Employee)
            .WhereIf(input.EmployeeId.HasValue, o => o.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeOnHoldDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeOnHoldForEditOutput> GetEmployeeOnHoldForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeOnHoldForEditOutput
        {
            EmployeeOnHold = new CreateOrEditEmployeeOnHoldDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                OnHold = entity.OnHold,
                OnHoldBasicPay = entity.OnHoldBasicPay,
                OnHoldLeavePay = entity.OnHoldLeavePay,
                OnHoldIndemnityPay = entity.OnHoldIndemnityPay,
                OnHoldAirTicketPay = entity.OnHoldAirTicketPay,
                OnHoldLoanDeduction = entity.OnHoldLoanDeduction,
                OnHoldAllEarnings = entity.OnHoldAllEarnings,
                OnHoldAllDeductions = entity.OnHoldAllDeductions,
                BankTransfer = entity.BankTransfer,
                Comments = entity.Comments,
                EmployeeActionId = entity.EmployeeActionId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeOnHoldDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeOnHoldDto input)
    {
        var entity = MapToEntity(input, new EmployeeOnHold());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeOnHoldDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Shared with the separation screen, which writes holds too (legacy: both paths ended in
    /// <c>EmployeeOnHold.OnSaving</c>). See <see cref="IEmployeeOnHoldService"/>.
    /// </summary>
    private Task ValidateAsync(EmployeeOnHold entity) => _onHoldService.ValidateAsync(entity);

    private static EmployeeOnHold MapToEntity(CreateOrEditEmployeeOnHoldDto input, EmployeeOnHold entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.OnHold = input.OnHold;
        entity.OnHoldBasicPay = input.OnHoldBasicPay;
        entity.OnHoldLeavePay = input.OnHoldLeavePay;
        entity.OnHoldIndemnityPay = input.OnHoldIndemnityPay;
        entity.OnHoldAirTicketPay = input.OnHoldAirTicketPay;
        entity.OnHoldLoanDeduction = input.OnHoldLoanDeduction;
        entity.OnHoldAllEarnings = input.OnHoldAllEarnings;
        entity.OnHoldAllDeductions = input.OnHoldAllDeductions;
        entity.BankTransfer = input.BankTransfer;
        entity.Comments = input.Comments;
        entity.EmployeeActionId = input.EmployeeActionId;
        return entity;
    }

    private static EmployeeOnHoldDto ToDto(EmployeeOnHold entity)
    {
        return new EmployeeOnHoldDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OnHold = entity.OnHold,
            OnHoldBasicPay = entity.OnHoldBasicPay,
            OnHoldLeavePay = entity.OnHoldLeavePay,
            OnHoldIndemnityPay = entity.OnHoldIndemnityPay,
            OnHoldAirTicketPay = entity.OnHoldAirTicketPay,
            OnHoldLoanDeduction = entity.OnHoldLoanDeduction,
            OnHoldAllEarnings = entity.OnHoldAllEarnings,
            OnHoldAllDeductions = entity.OnHoldAllDeductions,
            BankTransfer = entity.BankTransfer,
            Comments = entity.Comments,
            EmployeeActionId = entity.EmployeeActionId
        };
    }
}
