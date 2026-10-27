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
/// Application service for <see cref="ManageSelfServiceSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageSelfServiceSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- ported to <see cref="DeleteSelfServiceSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_SelfServiceSettings)]
public class ManageSelfServiceSettingsAppService : HCMv3AppServiceBase, IManageSelfServiceSettingsAppService
{
    private readonly IRepository<ManageSelfServiceSettings> _repository;

    public ManageSelfServiceSettingsAppService(IRepository<ManageSelfServiceSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<SelfServiceSettingsListDto>> GetSelfServiceSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SelfServiceSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<SelfServiceSettingsEditDto> GetSelfServiceSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_SelfServiceSettings_Create, AppPermissions.Pages_SystemSettings_SelfServiceSettings_Edit)]
    public async Task CreateOrEditSelfServiceSettings(SelfServiceSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_SelfServiceSettings_Delete)]
    public Task DeleteSelfServiceSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(SelfServiceSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageSelfServiceSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(SelfServiceSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageSelfServiceSettings MapToEntity(SelfServiceSettingsEditDto input, ManageSelfServiceSettings entity)
    {
        entity.RedirectAfterWFApproval = input.RedirectAfterWFApproval;
        entity.AllowPunchOutsideRange = input.AllowPunchOutsideRange;
        entity.AutoLogoutAfterPunch = input.AutoLogoutAfterPunch;
        entity.AllowGeolocationSelfie = input.AllowGeolocationSelfie;
        entity.HideGeolocationInfo = input.HideGeolocationInfo;
        entity.HideGeolocationPunchInfo = input.HideGeolocationPunchInfo;
        entity.EmailNotification = input.EmailNotification;
        entity.DefaultLoginPage = input.DefaultLoginPage;
        entity.CaptchaKey = input.CaptchaKey;
        entity.ShowLeaveInfo = input.ShowLeaveInfo;
        entity.ShowCurrentLeaveBalance = input.ShowCurrentLeaveBalance;
        entity.ShowUpdatedLeaveBalance = input.ShowUpdatedLeaveBalance;
        entity.ShowEmployeeInfo = input.ShowEmployeeInfo;
        entity.IsAttachmentAllowed = input.IsAttachmentAllowed;
        entity.IsAdditionalUnpaidLeaveAllowed = input.IsAdditionalUnpaidLeaveAllowed;
        entity.ShowPaymentDate = input.ShowPaymentDate;
        entity.EnablePayWithPayroll = input.EnablePayWithPayroll;
        entity.EnablePayInAdvance = input.EnablePayInAdvance;
        entity.IsPassportWithdrawalRequired = input.IsPassportWithdrawalRequired;
        entity.PassportWithdrawalSubmissionStep = input.PassportWithdrawalSubmissionStep;
        entity.IsWithdrawalReasonMandatory = input.IsWithdrawalReasonMandatory;

        return entity;
    }

    private static SelfServiceSettingsListDto ToListDto(ManageSelfServiceSettings entity)
    {
        return new SelfServiceSettingsListDto
        {
            Id = entity.Id,
            RedirectAfterWFApproval = entity.RedirectAfterWFApproval,
            AllowPunchOutsideRange = entity.AllowPunchOutsideRange,
            AutoLogoutAfterPunch = entity.AutoLogoutAfterPunch,
            AllowGeolocationSelfie = entity.AllowGeolocationSelfie,
            HideGeolocationInfo = entity.HideGeolocationInfo,
            HideGeolocationPunchInfo = entity.HideGeolocationPunchInfo,
            EmailNotification = entity.EmailNotification,
            DefaultLoginPage = entity.DefaultLoginPage,
            CaptchaKey = entity.CaptchaKey,
            ShowLeaveInfo = entity.ShowLeaveInfo,
            ShowCurrentLeaveBalance = entity.ShowCurrentLeaveBalance,
            ShowUpdatedLeaveBalance = entity.ShowUpdatedLeaveBalance,
            ShowEmployeeInfo = entity.ShowEmployeeInfo,
            IsAttachmentAllowed = entity.IsAttachmentAllowed,
            IsAdditionalUnpaidLeaveAllowed = entity.IsAdditionalUnpaidLeaveAllowed,
            ShowPaymentDate = entity.ShowPaymentDate,
            EnablePayWithPayroll = entity.EnablePayWithPayroll,
            EnablePayInAdvance = entity.EnablePayInAdvance,
            IsPassportWithdrawalRequired = entity.IsPassportWithdrawalRequired,
            PassportWithdrawalSubmissionStep = entity.PassportWithdrawalSubmissionStep,
            IsWithdrawalReasonMandatory = entity.IsWithdrawalReasonMandatory
        };
    }

    private static SelfServiceSettingsEditDto ToEditDto(ManageSelfServiceSettings entity)
    {
        return new SelfServiceSettingsEditDto
        {
            Id = entity.Id,
            RedirectAfterWFApproval = entity.RedirectAfterWFApproval,
            AllowPunchOutsideRange = entity.AllowPunchOutsideRange,
            AutoLogoutAfterPunch = entity.AutoLogoutAfterPunch,
            AllowGeolocationSelfie = entity.AllowGeolocationSelfie,
            HideGeolocationInfo = entity.HideGeolocationInfo,
            HideGeolocationPunchInfo = entity.HideGeolocationPunchInfo,
            EmailNotification = entity.EmailNotification,
            DefaultLoginPage = entity.DefaultLoginPage,
            CaptchaKey = entity.CaptchaKey,
            ShowLeaveInfo = entity.ShowLeaveInfo,
            ShowCurrentLeaveBalance = entity.ShowCurrentLeaveBalance,
            ShowUpdatedLeaveBalance = entity.ShowUpdatedLeaveBalance,
            ShowEmployeeInfo = entity.ShowEmployeeInfo,
            IsAttachmentAllowed = entity.IsAttachmentAllowed,
            IsAdditionalUnpaidLeaveAllowed = entity.IsAdditionalUnpaidLeaveAllowed,
            ShowPaymentDate = entity.ShowPaymentDate,
            EnablePayWithPayroll = entity.EnablePayWithPayroll,
            EnablePayInAdvance = entity.EnablePayInAdvance,
            IsPassportWithdrawalRequired = entity.IsPassportWithdrawalRequired,
            PassportWithdrawalSubmissionStep = entity.PassportWithdrawalSubmissionStep,
            IsWithdrawalReasonMandatory = entity.IsWithdrawalReasonMandatory
        };
    }
}
