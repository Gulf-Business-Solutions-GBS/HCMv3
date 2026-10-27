using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal rating scale master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PARating</c>). <see cref="Status"/> is the legacy
/// "Active" checkbox (field kept as <c>Status</c> for parity with the legacy property name).
/// </summary>
public class PARating : FullAuditedEntity, IMustHaveTenant
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

    /// <summary>When true, enforces mandatory comments for this rating's selection.</summary>
    public virtual bool IsCommentRequired { get; set; }

    /// <summary>Legacy "Active" checkbox (field name kept as <c>Status</c> for parity).</summary>
    public virtual bool Status { get; set; }

    /// <summary>Maximum allowable limit for ratings on a single KPI form.</summary>
    public virtual int MaxLimit { get; set; }

    [StringLength(4000)]
    public virtual string DescriptionEn { get; set; }

    [StringLength(4000)]
    public virtual string DescriptionAr { get; set; }

    public virtual double FromRange { get; set; }

    public virtual double ToRange { get; set; }
}
