using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Marital-status-based allowance step of a <see cref="SocialSecurity"/> master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.SocialSecurityMaritalAllowance</c>), listed
/// separately and linked back via <see cref="SocialSecurityId"/>.
/// </summary>
public class SocialSecurityMaritalAllowance : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SocialSecurityId { get; set; }

    public virtual SocialSecurity SocialSecurity { get; set; }

    public virtual int EducationBranchId { get; set; }

    public virtual EducationBranch EducationBranch { get; set; }

    public virtual int MaritalStatusId { get; set; }

    public virtual MaritalStatus MaritalStatus { get; set; }

    public virtual double Amount { get; set; }
}
