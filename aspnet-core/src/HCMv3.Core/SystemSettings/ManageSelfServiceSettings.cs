using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide employee self-service configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageSelfServiceSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageSelfServiceSettingsAppService.DeleteSelfServiceSettings</c>,
/// not here. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManageSelfServiceSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual RedirectAfterWFApproval RedirectAfterWFApproval { get; set; }

    public virtual bool AllowPunchOutsideRange { get; set; }

    public virtual bool AutoLogoutAfterPunch { get; set; }

    public virtual bool AllowGeolocationSelfie { get; set; }

    public virtual bool HideGeolocationInfo { get; set; }

    public virtual bool HideGeolocationPunchInfo { get; set; }

    public virtual bool EmailNotification { get; set; }

    public virtual string DefaultLoginPage { get; set; }

    public virtual string CaptchaKey { get; set; }

    public virtual bool ShowLeaveInfo { get; set; }

    public virtual bool ShowCurrentLeaveBalance { get; set; }

    public virtual bool ShowUpdatedLeaveBalance { get; set; }

    public virtual bool ShowEmployeeInfo { get; set; }

    public virtual bool IsAttachmentAllowed { get; set; }

    public virtual bool IsAdditionalUnpaidLeaveAllowed { get; set; }

    public virtual bool ShowPaymentDate { get; set; }

    public virtual bool EnablePayWithPayroll { get; set; }

    public virtual bool EnablePayInAdvance { get; set; }

    public virtual bool IsPassportWithdrawalRequired { get; set; }

    public virtual int PassportWithdrawalSubmissionStep { get; set; }
}
