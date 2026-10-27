using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeFnFPayment"/> (ported from legacy XAF EmployeeFnFPayment).
///
/// <para>Saving and deleting run legacy's F&amp;F side effects through <see cref="IFullAndFinalSettlementService"/>:
/// the validations, the indemnity (<c>EmployeeGratuity</c>) row the end-of-service off-cycle pays from, leave
/// encashment, ticket compensation, the indemnity pay adjustment, and the employee going inactive.
/// The asset check, bonus pay, loans, the position and the login go through
/// <see cref="IFullAndFinalExitService"/>, called from the settlement service at legacy's own points.</para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments)]
public class EmployeeFnFPaymentsAppService : HCMv3AppServiceBase, IEmployeeFnFPaymentsAppService
{
    private readonly IRepository<EmployeeFnFPayment> _repository;
    private readonly IFullAndFinalSettlementService _settlementService;

    public EmployeeFnFPaymentsAppService(
        IRepository<EmployeeFnFPayment> repository,
        IFullAndFinalSettlementService settlementService)
    {
        _repository = repository;
        _settlementService = settlementService;
    }

    public async Task<FullAndFinalBalancesDto> GetFullAndFinalBalances(EntityDto input)
    {
        var balances = await _settlementService.GetBalancesAsync(input.Id);

        return new FullAndFinalBalancesDto
        {
            LeaveAccrualDays = balances.LeaveAccrualDays,
            LeaveRemainingDays = balances.LeaveRemainingDays,
            GratuityAccrualDays = balances.GratuityAccrualDays,
            GratuityRemainingDays = balances.GratuityRemainingDays,
            SuggestedIndemnityDays = balances.SuggestedIndemnityDays,
            TicketAccrualCount = balances.TicketAccrualCount,
            TicketRemainingCount = balances.TicketRemainingCount,
            Dependant1TicketCount = balances.Dependant1TicketCount,
            Dependant1TicketRemainingCount = balances.Dependant1TicketRemainingCount,
            Dependant2TicketCount = balances.Dependant2TicketCount,
            Dependant2TicketRemainingCount = balances.Dependant2TicketRemainingCount,
            Dependant3TicketCount = balances.Dependant3TicketCount,
            Dependant3TicketRemainingCount = balances.Dependant3TicketRemainingCount,
            AutoFillLeaveCompensateDays = balances.AutoFillLeaveCompensateDays,
            AutoFillGratuityCompensateDays = balances.AutoFillGratuityCompensateDays,
            LoanDueAmount = balances.LoanDueAmount,
            LoanBalanceDueAmount = balances.LoanBalanceDueAmount,
            SuggestedBonusPay = balances.SuggestedBonusPay
        };
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
        var entity = await _repository.GetAsync(input.Id);
        var previous = ToSettlementValues(entity);

        await _settlementService.ValidateDeleteAsync(previous, entity.Id);
        await _settlementService.UndoAsync(previous);

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeFnFPaymentDto input)
    {
        var entity = MapToEntity(input, new EmployeeFnFPayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        var current = ToSettlementValues(entity, input.SettleLoanInEndOfService);
        await _settlementService.ValidateSaveAsync(current, previous: null, currentFnFPaymentId: null);

        await _repository.InsertAsync(entity);
        await _settlementService.ApplyAsync(current, previous: null);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeFnFPaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy's prev* fields (captured in OnLoaded): what the last save wrote must be undone first.
        var previous = ToSettlementValues(entity);

        MapToEntity(input, entity);
        var current = ToSettlementValues(entity, input.SettleLoanInEndOfService);

        await _settlementService.ValidateSaveAsync(current, previous, entity.Id);

        await _repository.UpdateAsync(entity);
        await _settlementService.ApplyAsync(current, previous);
    }

    /// <summary>
    /// The values one save works with. <paramref name="settleLoanInEndOfService"/> is legacy's non-persisted tick
    /// box, so it comes from the input and is never true for the PREVIOUS values.
    /// </summary>
    private static FullAndFinalValues ToSettlementValues(EmployeeFnFPayment entity, bool settleLoanInEndOfService = false)
    {
        return new FullAndFinalValues
        {
            SettleLoanInEndOfService = settleLoanInEndOfService,
            EmployeeId = entity.EmployeeId,
            CompensationDate = entity.CompensationDate,
            LeaveCompensateDays = entity.LeaveCompensateDays,
            GratuityCompensateDays = entity.GratuityCompensateDays,
            IndemnityPayAdjustment = entity.IndemnityPayAdjustment,
            TicketCompensateCount = entity.TicketCompensateCount,
            Dependant1TicketCompensateCount = entity.Dependant1TicketCompensateCount,
            Dependant2TicketCompensateCount = entity.Dependant2TicketCompensateCount,
            Dependant3TicketCompensateCount = entity.Dependant3TicketCompensateCount,
            CompensateBonusPay = entity.CompensateBonusPay
        };
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
