using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManagePayrollSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so <see cref="DeletePayrollSettings"/>
/// always throws here too. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollSettings)]
public class ManagePayrollSettingsAppService : HCMv3AppServiceBase, IManagePayrollSettingsAppService
{
    private readonly IRepository<ManagePayrollSettings> _repository;

    public ManagePayrollSettingsAppService(IRepository<ManagePayrollSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<PayrollSettingsListDto>> GetPayrollSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<PayrollSettingsEditDto> GetPayrollSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditPayrollSettings(PayrollSettingsEditDto input)
    {
        if (input.Id != 0)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollSettings_Delete)]
    public Task DeletePayrollSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollSettings_Create)]
    protected virtual async Task CreateAsync(PayrollSettingsEditDto input)
    {
        var entity = new ManagePayrollSettings();
        MapToEntity(input, entity);
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_PayrollSettings_Edit)]
    protected virtual async Task UpdateAsync(PayrollSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static void MapToEntity(PayrollSettingsEditDto input, ManagePayrollSettings entity)
    {
        entity.ClientId = input.ClientId;
        entity.DefaultWorkingHours = input.DefaultWorkingHours;
        entity.PayslipSignature = input.PayslipSignature;
        entity.LeaveAdvPaymentdateBefore15 = input.LeaveAdvPaymentdateBefore15;
        entity.LeaveAdvPaymentdateAfter15 = input.LeaveAdvPaymentdateAfter15;
        entity.LeaveProvisionAsPerCurrentSalary = input.LeaveProvisionAsPerCurrentSalary;
        entity.EnableNotReturnFromLeaveLogic = input.EnableNotReturnFromLeaveLogic;
        entity.EnableBasicpayMoreThenFixedDays = input.EnableBasicpayMoreThenFixedDays;
        entity.EnableUnpaidDedSlabWise = input.EnableUnpaidDedSlabWise;
        entity.EnableFixedDaysLogicChange = input.EnableFixedDaysLogicChange;
        entity.GenerateRegisterOnNotReturnFromLeave = input.GenerateRegisterOnNotReturnFromLeave;
        entity.OffcycleBankFileCutoff = input.OffcycleBankFileCutoff;
        entity.RoundNetPay = input.RoundNetPay;
        entity.ApplyAnnualleaveNoImpactLogic = input.ApplyAnnualleaveNoImpactLogic;
        entity.EnablePayrollEngineAutorun = input.EnablePayrollEngineAutorun;
        entity.Decimal6RoundingFrom = input.Decimal6RoundingFrom;
        entity.FullMonthLeaveAdjForFixedDays = input.FullMonthLeaveAdjForFixedDays;
        entity.PayLeaveAsPerCutoff = input.PayLeaveAsPerCutoff;
        entity.DecimalInSalaryRegister = input.DecimalInSalaryRegister;
        entity.DefaultLeavePaymentType = input.DefaultLeavePaymentType;
        entity.AllowDeleteBackdatedSalaryRegister = input.AllowDeleteBackdatedSalaryRegister;
        entity.AutoFillFnFCompensation = input.AutoFillFnFCompensation;
        entity.EditableAutoFillFnFCompensation = input.EditableAutoFillFnFCompensation;
        entity.FixedToActualWhenLeaveOverlapOnMonthStart = input.FixedToActualWhenLeaveOverlapOnMonthStart;
        entity.FixedToActualWhenLeaveOverlapOnMonthEnd = input.FixedToActualWhenLeaveOverlapOnMonthEnd;
        entity.FixedToActualWhenUpaidLeaveFound = input.FixedToActualWhenUpaidLeaveFound;
        entity.FixedToActualWhenAbsentDayFound = input.FixedToActualWhenAbsentDayFound;
        entity.OvertimeHoursBasis = input.OvertimeHoursBasis;
        entity.CarryAllowanceOnSalaryImport = input.CarryAllowanceOnSalaryImport;
        entity.SocialSecurityRateRule = input.SocialSecurityRateRule;
        entity.AllowHealthReimburseBalCarryFwd = input.AllowHealthReimburseBalCarryFwd;
        entity.AllowSimulationRun = input.AllowSimulationRun;
        entity.RunOffCycleForFutureDate = input.RunOffCycleForFutureDate;
        entity.DisableRetroCalculation = input.DisableRetroCalculation;
        entity.PickOffcyclePayment = input.PickOffcyclePayment;
        entity.RegularLeaveEncashOnBasicPayOnly = input.RegularLeaveEncashOnBasicPayOnly;
        entity.FnFLeaveEncashOnBasicPayOnly = input.FnFLeaveEncashOnBasicPayOnly;
        entity.ShowBillingTypeInPaymentScreen = input.ShowBillingTypeInPaymentScreen;
        entity.AutoUpdateLoanRecoveryEndDate = input.AutoUpdateLoanRecoveryEndDate;
        entity.ChangePartialPeriodFactoringInFixedDaysBilling = input.ChangePartialPeriodFactoringInFixedDaysBilling;
        entity.PartialPeriodCutoffDayInFixedDaysBilling = input.PartialPeriodCutoffDayInFixedDaysBilling;
        entity.EnableEffectiveDateInRecPayment = input.EnableEffectiveDateInRecPayment;
        entity.BankFileMinAmount = input.BankFileMinAmount;
        entity.IncludeSalaryIncProvDiffInSalReg = input.IncludeSalaryIncProvDiffInSalReg;
        entity.IncludeSalaryIncProvDiffInSalRegEffDt = input.IncludeSalaryIncProvDiffInSalRegEffDt;
        entity.LockSalaryRegisterOnPayrollExit = input.LockSalaryRegisterOnPayrollExit;
    }

    private static PayrollSettingsListDto ToListDto(ManagePayrollSettings entity)
    {
        return new PayrollSettingsListDto
        {
            Id = entity.Id,
            ClientId = entity.ClientId,
            DefaultWorkingHours = entity.DefaultWorkingHours,
            PayslipSignature = entity.PayslipSignature
        };
    }

    private static PayrollSettingsEditDto ToEditDto(ManagePayrollSettings entity)
    {
        return new PayrollSettingsEditDto
        {
            Id = entity.Id,
            ClientId = entity.ClientId,
            DefaultWorkingHours = entity.DefaultWorkingHours,
            PayslipSignature = entity.PayslipSignature,
            LeaveAdvPaymentdateBefore15 = entity.LeaveAdvPaymentdateBefore15,
            LeaveAdvPaymentdateAfter15 = entity.LeaveAdvPaymentdateAfter15,
            LeaveProvisionAsPerCurrentSalary = entity.LeaveProvisionAsPerCurrentSalary,
            EnableNotReturnFromLeaveLogic = entity.EnableNotReturnFromLeaveLogic,
            EnableBasicpayMoreThenFixedDays = entity.EnableBasicpayMoreThenFixedDays,
            EnableUnpaidDedSlabWise = entity.EnableUnpaidDedSlabWise,
            EnableFixedDaysLogicChange = entity.EnableFixedDaysLogicChange,
            GenerateRegisterOnNotReturnFromLeave = entity.GenerateRegisterOnNotReturnFromLeave,
            OffcycleBankFileCutoff = entity.OffcycleBankFileCutoff,
            RoundNetPay = entity.RoundNetPay,
            ApplyAnnualleaveNoImpactLogic = entity.ApplyAnnualleaveNoImpactLogic,
            EnablePayrollEngineAutorun = entity.EnablePayrollEngineAutorun,
            Decimal6RoundingFrom = entity.Decimal6RoundingFrom,
            FullMonthLeaveAdjForFixedDays = entity.FullMonthLeaveAdjForFixedDays,
            PayLeaveAsPerCutoff = entity.PayLeaveAsPerCutoff,
            DecimalInSalaryRegister = entity.DecimalInSalaryRegister,
            DefaultLeavePaymentType = entity.DefaultLeavePaymentType,
            AllowDeleteBackdatedSalaryRegister = entity.AllowDeleteBackdatedSalaryRegister,
            AutoFillFnFCompensation = entity.AutoFillFnFCompensation,
            EditableAutoFillFnFCompensation = entity.EditableAutoFillFnFCompensation,
            FixedToActualWhenLeaveOverlapOnMonthStart = entity.FixedToActualWhenLeaveOverlapOnMonthStart,
            FixedToActualWhenLeaveOverlapOnMonthEnd = entity.FixedToActualWhenLeaveOverlapOnMonthEnd,
            FixedToActualWhenUpaidLeaveFound = entity.FixedToActualWhenUpaidLeaveFound,
            FixedToActualWhenAbsentDayFound = entity.FixedToActualWhenAbsentDayFound,
            OvertimeHoursBasis = entity.OvertimeHoursBasis,
            CarryAllowanceOnSalaryImport = entity.CarryAllowanceOnSalaryImport,
            SocialSecurityRateRule = entity.SocialSecurityRateRule,
            AllowHealthReimburseBalCarryFwd = entity.AllowHealthReimburseBalCarryFwd,
            AllowSimulationRun = entity.AllowSimulationRun,
            RunOffCycleForFutureDate = entity.RunOffCycleForFutureDate,
            DisableRetroCalculation = entity.DisableRetroCalculation,
            PickOffcyclePayment = entity.PickOffcyclePayment,
            RegularLeaveEncashOnBasicPayOnly = entity.RegularLeaveEncashOnBasicPayOnly,
            FnFLeaveEncashOnBasicPayOnly = entity.FnFLeaveEncashOnBasicPayOnly,
            ShowBillingTypeInPaymentScreen = entity.ShowBillingTypeInPaymentScreen,
            AutoUpdateLoanRecoveryEndDate = entity.AutoUpdateLoanRecoveryEndDate,
            ChangePartialPeriodFactoringInFixedDaysBilling = entity.ChangePartialPeriodFactoringInFixedDaysBilling,
            PartialPeriodCutoffDayInFixedDaysBilling = entity.PartialPeriodCutoffDayInFixedDaysBilling,
            EnableEffectiveDateInRecPayment = entity.EnableEffectiveDateInRecPayment,
            BankFileMinAmount = entity.BankFileMinAmount,
            IncludeSalaryIncProvDiffInSalReg = entity.IncludeSalaryIncProvDiffInSalReg,
            IncludeSalaryIncProvDiffInSalRegEffDt = entity.IncludeSalaryIncProvDiffInSalRegEffDt,
            LockSalaryRegisterOnPayrollExit = entity.LockSalaryRegisterOnPayrollExit
        };
    }
}
