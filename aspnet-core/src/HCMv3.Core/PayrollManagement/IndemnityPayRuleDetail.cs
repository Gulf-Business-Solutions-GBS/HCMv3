using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Year-of-service pay-percentage step for an <see cref="IndemnityPayRule"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.IndemnityPayRuleDetail</c>), listed separately and
/// linked back via <see cref="IndemnityPayRuleId"/>.
/// </summary>
public class IndemnityPayRuleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int IndemnityPayRuleId { get; set; }

    public virtual IndemnityPayRule IndemnityPayRule { get; set; }

    public virtual double MinYearOfService { get; set; }

    public virtual double MaxYearOfService { get; set; }

    public virtual double Number { get; set; }

    public virtual Unit Unit { get; set; }

    public virtual double PayPercentage { get; set; }
}
