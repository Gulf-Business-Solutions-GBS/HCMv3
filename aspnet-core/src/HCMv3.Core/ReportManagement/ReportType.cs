using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Report-builder report type lookup (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportType</c>). No <c>NameAr</c> in the legacy object,
/// so NOT derived from <see cref="SimpleLookupEntity"/>.
/// </summary>
public class ReportType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }
}
