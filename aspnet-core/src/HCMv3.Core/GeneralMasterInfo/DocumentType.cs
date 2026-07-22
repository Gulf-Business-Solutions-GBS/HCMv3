using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DocumentType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DocumentType</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="NotificationConfig"/> FK and notification
/// scheduling fields, so it can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the
/// same bespoke pattern). The legacy aggregated <c>PrintForm</c> collection is ported separately as
/// <see cref="DocumentPrintForm"/>.
/// </summary>
public class DocumentType : FullAuditedEntity, IMustHaveTenant
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

    public virtual int NotificationPeriod { get; set; }

    public virtual int? NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }

    public virtual NotificationFrequency NotificationFrequency { get; set; }

    public virtual int NotificationCountLimit { get; set; }

    public virtual DocumentBelongsTo DocumentBelongsTo { get; set; }
}
