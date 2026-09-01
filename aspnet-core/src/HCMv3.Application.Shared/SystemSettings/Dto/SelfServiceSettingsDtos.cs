using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class SelfServiceSettingsListDto : EntityDto
{
    public RedirectAfterWFApproval RedirectAfterWFApproval { get; set; }

    public bool AllowPunchOutsideRange { get; set; }

    public bool AutoLogoutAfterPunch { get; set; }

    public bool AllowGeolocationSelfie { get; set; }

    public bool HideGeolocationInfo { get; set; }

    public bool HideGeolocationPunchInfo { get; set; }

    public bool EmailNotification { get; set; }

    public string DefaultLoginPage { get; set; }

    public string CaptchaKey { get; set; }

    public bool ShowLeaveInfo { get; set; }

    public bool ShowCurrentLeaveBalance { get; set; }

    public bool ShowUpdatedLeaveBalance { get; set; }

    public bool ShowEmployeeInfo { get; set; }

    public bool IsAttachmentAllowed { get; set; }

    public bool IsAdditionalUnpaidLeaveAllowed { get; set; }

    public bool ShowPaymentDate { get; set; }

    public bool EnablePayWithPayroll { get; set; }

    public bool EnablePayInAdvance { get; set; }

    public bool IsPassportWithdrawalRequired { get; set; }

    public int PassportWithdrawalSubmissionStep { get; set; }

    public bool IsWithdrawalReasonMandatory { get; set; }
}

public class SelfServiceSettingsEditDto : EntityDto
{
    public RedirectAfterWFApproval RedirectAfterWFApproval { get; set; }

    public bool AllowPunchOutsideRange { get; set; }

    public bool AutoLogoutAfterPunch { get; set; }

    public bool AllowGeolocationSelfie { get; set; }

    public bool HideGeolocationInfo { get; set; }

    public bool HideGeolocationPunchInfo { get; set; }

    public bool EmailNotification { get; set; }

    public string DefaultLoginPage { get; set; }

    public string CaptchaKey { get; set; }

    public bool ShowLeaveInfo { get; set; }

    public bool ShowCurrentLeaveBalance { get; set; }

    public bool ShowUpdatedLeaveBalance { get; set; }

    public bool ShowEmployeeInfo { get; set; }

    public bool IsAttachmentAllowed { get; set; }

    public bool IsAdditionalUnpaidLeaveAllowed { get; set; }

    public bool ShowPaymentDate { get; set; }

    public bool EnablePayWithPayroll { get; set; }

    public bool EnablePayInAdvance { get; set; }

    public bool IsPassportWithdrawalRequired { get; set; }

    public int PassportWithdrawalSubmissionStep { get; set; }

    public bool IsWithdrawalReasonMandatory { get; set; }
}
