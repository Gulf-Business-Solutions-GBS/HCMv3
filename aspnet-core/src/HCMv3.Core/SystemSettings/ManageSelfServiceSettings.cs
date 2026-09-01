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

    /// <summary>
    /// Whether an employee must say why they need a document withdrawn.
    ///
    /// <para>
    /// Legacy kept this outside the settings table entirely, in an ESS-side JSON file
    /// (<c>App_Data/ModuleSettings.json</c>, read by
    /// <c>PassportWithdrawalReqRepository.GetPassportWithdrawalRequestsSettings</c>), which meant it could
    /// not differ per tenant and could not be changed without touching the server. It is a per-tenant
    /// self-service policy, so it is stored as one here alongside the other passport-withdrawal settings.
    /// </para>
    /// <para>
    /// Defaults to false where legacy's hard-coded fallback was true. A bool column added to existing rows
    /// is false either way, and quietly making a previously optional field mandatory on upgrade would start
    /// refusing requests that used to go through. Tenants that want it switch it on.
    /// </para>
    /// </summary>
    public virtual bool IsWithdrawalReasonMandatory { get; set; }
}
