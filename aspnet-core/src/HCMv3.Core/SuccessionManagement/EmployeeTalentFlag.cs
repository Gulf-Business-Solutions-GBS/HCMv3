using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Flight-risk/retention flag for an employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmployeeTalentFlag</c>). The legacy StartDate/EndDate
/// overlap check was already commented out (dead code) in the source XAF business object -- not reproduced
/// here, only the required-field validation is.
/// </summary>
public class EmployeeTalentFlag : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int RiskOfLossId { get; set; }

    public virtual RiskOfLoss RiskOfLoss { get; set; }

    public virtual int ImpactOfLossId { get; set; }

    public virtual ImpactOfLoss ImpactOfLoss { get; set; }

    public virtual int ReasonForLeavingId { get; set; }

    public virtual ReasonForLeaving ReasonForLeaving { get; set; }

    public virtual int TalentPoolId { get; set; }

    public virtual TalentPool TalentPool { get; set; }
}
