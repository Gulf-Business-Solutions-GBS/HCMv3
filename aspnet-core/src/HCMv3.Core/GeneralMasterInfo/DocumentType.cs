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

    /// <summary>
    /// Whether this document type is a LETTER the company issues to the employee -- a salary certificate, an
    /// experience certificate, a to-whom-it-may-concern letter -- as opposed to a document the employee
    /// holds and the company merely records, such as a passport or a civil ID. Only rows with this set are
    /// offered on the letter-request screen (<c>EmployeeLetterRequest</c>).
    ///
    /// <para>
    /// <b>This is the legacy <c>AllowUpdate</c> column, renamed and INVERTED on purpose.</b> Legacy filtered
    /// its letter-type dropdown with <c>Where(r =&gt; r.AllowUpdate == false)</c>, so "is issuable as a
    /// letter" was expressed as the absence of an unrelated-sounding permission flag. Reading that filter
    /// tells you nothing about why a passport is excluded. Stated positively and named for what it selects,
    /// the rule is legible: <c>IsLetterType == true</c> is offered, everything else is not. Data migrated
    /// from legacy therefore sets <c>IsLetterType = !AllowUpdate</c>.
    /// </para>
    ///
    /// <para>
    /// Defaults to <c>false</c>, which is the safe direction: a document type nobody has classified yet does
    /// not silently appear as something an employee can request a letter for.
    /// </para>
    /// </summary>
    public virtual bool IsLetterType { get; set; }
}
