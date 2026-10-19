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
/// Application service for <see cref="EmployeeSeparation"/> -- DELIBERATELY MINIMAL "record-keeping only"
/// port of the legacy XAF EmployeeSeparation business object (Wave B). See the entity class remarks for
/// the full list of dropped legacy behaviors (TimeRegister deletion, payroll-retro, advance-EoS indemnity
/// accrual, FnF/Rejoin chain-blocking, interdependent date recompute). Required-field and
/// LastDayInService &gt;= SubmissionDate validation is ported, and so is the on-hold block: saving the
/// separation's on-hold flags creates/replaces/removes the matching <see cref="EmployeeOnHold"/> row through
/// <see cref="ISeparationOnHoldService"/>, as legacy <c>SaveOtherInfo</c>/<c>OnDeleting</c> did.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations)]
public class EmployeeSeparationsAppService : HCMv3AppServiceBase, IEmployeeSeparationsAppService
{
    private readonly IRepository<EmployeeSeparation> _repository;

    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    private readonly ISeparationOnHoldService _separationOnHoldService;

    public EmployeeSeparationsAppService(
        IRepository<EmployeeSeparation> repository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        ISeparationOnHoldService separationOnHoldService)
    {
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _separationOnHoldService = separationOnHoldService;
    }

    /// <summary>
    /// Legacy <c>EmployeeSeparation.RecomputeNoticePeriod</c>: the notice period is the days from the
    /// submission date to the last day in service, counting the submission day unless
    /// <c>ManageGeneralSettings.NoticePeriodDaysExcludingSubmissionDate</c> is on; a separation ending the day
    /// it was submitted has a notice period of one day.
    /// </summary>
    private async Task ApplyNoticePeriodAsync(EmployeeSeparation entity)
    {
        if (entity.LastDayInService == default || entity.LastDayInService.Date == entity.SubmissionDate.Date)
        {
            entity.NoticePeriodDays = 1;
            return;
        }

        var generalSettings = await _sysSettingsAccessor.GetGeneralSettingsAsync();
        var excludeSubmissionDate = generalSettings != null && generalSettings.NoticePeriodDaysExcludingSubmissionDate;
        var days = (int)entity.LastDayInService.Date.Subtract(entity.SubmissionDate.Date).TotalDays;

        entity.NoticePeriodDays = excludeSubmissionDate ? days : days + 1;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSeparationDto>> GetAll(GetAllEmployeeSeparationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .Include(s => s.SeparationReason)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSeparationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSeparationForEditOutput> GetEmployeeSeparationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSeparationForEditOutput
        {
            EmployeeSeparation = new CreateOrEditEmployeeSeparationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                SubmissionDate = entity.SubmissionDate,
                LastDayInService = entity.LastDayInService,
                NoticePeriodDays = entity.NoticePeriodDays,
                PlannedLastDayInService = entity.PlannedLastDayInService,
                SeparationType = entity.SeparationType,
                SeparationReasonId = entity.SeparationReasonId,
                RevokeDate = entity.RevokeDate,
                OnHoldFromDate = entity.OnHoldFromDate,
                OnHoldTillDate = entity.OnHoldTillDate,
                OnHoldAll = entity.OnHoldAll,
                OnHoldBasicPay = entity.OnHoldBasicPay,
                OnHoldLeavePay = entity.OnHoldLeavePay,
                OnHoldIndemnityPay = entity.OnHoldIndemnityPay,
                OnHoldAirTicketPay = entity.OnHoldAirTicketPay,
                OnHoldLoanDeduction = entity.OnHoldLoanDeduction,
                OnHoldAllEarnings = entity.OnHoldAllEarnings,
                OnHoldAllDeductions = entity.OnHoldAllDeductions,
                BankTransfer = entity.BankTransfer,
                RevokeComments = entity.RevokeComments,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSeparationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSeparationDto input)
    {
        var entity = MapToEntity(input, new EmployeeSeparation());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ApplyNoticePeriodAsync(entity);
        Validate(entity);

        // Legacy order: PerformValidations, then SaveOtherInfo. A new separation has no previous values.
        await _separationOnHoldService.EnsureNoLaterOnHoldAsync(entity, null, null);
        await _separationOnHoldService.ApplyAsync(entity, null, null);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSeparationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy prevEmployee/prevOnHoldFromDate (OnLoaded): the values as stored, before this edit.
        var previousEmployeeId = entity.EmployeeId;
        var previousOnHoldFromDate = entity.OnHoldFromDate;

        MapToEntity(input, entity);

        await ApplyNoticePeriodAsync(entity);
        Validate(entity);

        await _separationOnHoldService.EnsureNoLaterOnHoldAsync(entity, previousEmployeeId, previousOnHoldFromDate);
        await _separationOnHoldService.ApplyAsync(entity, previousEmployeeId, previousOnHoldFromDate);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        // Legacy OnDeleting: ValidatePreviousEntry, then delete the hold this separation created.
        await _separationOnHoldService.EnsureNoLaterOnHoldAsync(null, entity.EmployeeId, entity.OnHoldFromDate);
        await _separationOnHoldService.RemoveAsync(entity.EmployeeId, entity.OnHoldFromDate);

        await _repository.DeleteAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity (minimal): required fields already enforced by DTO Data
    /// Annotations; ports the LastDayInService &gt;= SubmissionDate check.
    /// </summary>
    private void Validate(EmployeeSeparation entity)
    {
        if (entity.LastDayInService.Date < entity.SubmissionDate.Date)
        {
            throw new UserFriendlyException(L("LastDayInServiceCannotBeEarlierThanSubmissionDate"));
        }
    }

    private static EmployeeSeparation MapToEntity(CreateOrEditEmployeeSeparationDto input, EmployeeSeparation entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.SubmissionDate = input.SubmissionDate.Date;
        entity.LastDayInService = input.LastDayInService.Date;
        entity.NoticePeriodDays = input.NoticePeriodDays;
        entity.PlannedLastDayInService = input.PlannedLastDayInService == default
            ? input.LastDayInService.Date
            : input.PlannedLastDayInService.Date;
        entity.SeparationType = input.SeparationType;
        entity.SeparationReasonId = input.SeparationReasonId;
        entity.RevokeDate = input.RevokeDate?.Date;
        entity.OnHoldFromDate = input.OnHoldFromDate?.Date;
        entity.OnHoldTillDate = input.OnHoldTillDate?.Date;
        entity.OnHoldAll = input.OnHoldAll;
        entity.OnHoldBasicPay = input.OnHoldBasicPay;
        entity.OnHoldLeavePay = input.OnHoldLeavePay;
        entity.OnHoldIndemnityPay = input.OnHoldIndemnityPay;
        entity.OnHoldAirTicketPay = input.OnHoldAirTicketPay;
        entity.OnHoldLoanDeduction = input.OnHoldLoanDeduction;
        entity.OnHoldAllEarnings = input.OnHoldAllEarnings;
        entity.OnHoldAllDeductions = input.OnHoldAllDeductions;
        entity.BankTransfer = input.BankTransfer;
        entity.RevokeComments = input.RevokeComments;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeSeparationDto ToDto(EmployeeSeparation entity)
    {
        return new EmployeeSeparationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            SubmissionDate = entity.SubmissionDate,
            LastDayInService = entity.LastDayInService,
            NoticePeriodDays = entity.NoticePeriodDays,
            PlannedLastDayInService = entity.PlannedLastDayInService,
            SeparationType = entity.SeparationType,
            SeparationReasonId = entity.SeparationReasonId,
            SeparationReasonName = entity.SeparationReason?.NameEn,
            RevokeDate = entity.RevokeDate,
            OnHoldFromDate = entity.OnHoldFromDate,
            OnHoldTillDate = entity.OnHoldTillDate,
            RevokeComments = entity.RevokeComments,
            Comments = entity.Comments
        };
    }
}
