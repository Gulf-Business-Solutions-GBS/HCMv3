using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal form-type master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.FormType</c>). Extends the Code/NameEn/NameAr
/// shape with reviewer-count limits, so it can't derive from <see cref="SimpleLookupEntity"/> (same
/// bespoke pattern as <c>GovtRelationsManagement.GovtJobTitle</c>).
/// </summary>
public class FormType : FullAuditedEntity, IMustHaveTenant
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

    public virtual int MinReviewerLimit { get; set; }

    public virtual int MaxReviewerLimit { get; set; }
}
