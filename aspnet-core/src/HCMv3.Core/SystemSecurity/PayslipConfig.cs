using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Singleton-style configuration pointing at the EmailTemplate used for payslip emails
/// (ported from legacy XAF HCM10.Module.BusinessObjects.SysSecurity.PayslipConfig). Not
/// architecturally enforced as a true singleton table -- PayslipConfigAppService.ValidateAsync
/// mirrors the legacy PerformValidation "only one row allowed" check instead.
/// </summary>
public class PayslipConfig : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmailTemplateId { get; set; }

    public virtual EmailTemplate EmailTemplate { get; set; }
}
