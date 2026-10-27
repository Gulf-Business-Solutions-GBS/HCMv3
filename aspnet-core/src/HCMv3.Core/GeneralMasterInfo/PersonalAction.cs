using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// PersonalAction master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.PersonalAction</c>).
/// Extends the Code/NameEn/NameAr shape with a set of requirement flags, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class PersonalAction : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual bool ReqOrgAssignment { get; set; }

    public virtual bool ReqBasicPay { get; set; }

    public virtual bool ReqContract { get; set; }

    public virtual bool ReqManager { get; set; }

    public virtual bool ReqOnHold { get; set; }

    public virtual bool ReqSeparation { get; set; }

    public virtual bool ReqFnFPayment { get; set; }

    public virtual bool IncludeInWorkflow { get; set; }

    public virtual bool IsAttachmentRequired { get; set; }
}
