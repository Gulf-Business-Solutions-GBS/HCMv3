using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Earnings-tier contribution step of a <see cref="SocialSecurity"/> master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SocialSecurityDetail</c>), listed separately and
/// linked back via <see cref="SocialSecurityId"/>.
/// </summary>
public class SocialSecurityDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SocialSecurityId { get; set; }

    public virtual SocialSecurity SocialSecurity { get; set; }

    public virtual double MinEarnings { get; set; }

    public virtual double MaxEarnings { get; set; }

    public virtual double EmployeeContribution { get; set; }

    public virtual double EmployerContribution { get; set; }

    public virtual double MinEarningsEmployeeContribution { get; set; }
}
