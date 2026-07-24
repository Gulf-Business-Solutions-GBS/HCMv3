using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Government job title master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.GovtJobTitle</c>). Extends the Code/NameEn/NameAr
/// shape with a <see cref="GovtJobCategory"/> FK, so it can't derive from <see cref="SimpleLookupEntity"/>
/// (see <see cref="Nationality"/> for the same bespoke pattern).
/// </summary>
public class GovtJobTitle : FullAuditedEntity, IMustHaveTenant
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

    public virtual int GovtJobCategoryId { get; set; }

    public virtual GovtJobCategory GovtJobCategory { get; set; }
}
