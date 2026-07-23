using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Education-based allowance step of a <see cref="SocialSecurity"/> master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SocialSecurityEducationAllowance</c>), listed
/// separately and linked back via <see cref="SocialSecurityId"/>.
/// </summary>
public class SocialSecurityEducationAllowance : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SocialSecurityId { get; set; }

    public virtual SocialSecurity SocialSecurity { get; set; }

    public virtual int EducationBranchId { get; set; }

    public virtual EducationBranch EducationBranch { get; set; }

    public virtual int EducationId { get; set; }

    public virtual Education Education { get; set; }

    public virtual double Amount { get; set; }
}
