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

    /// <summary>
    /// Whether employees may ask to take a document of this type out of company custody and hand it back --
    /// the passport, in most tenants. Legacy named one type by its code (a setting defaulting to "2"); here
    /// it is a switch on the type, so any type can be made withdrawable and more than one can be.
    ///
    /// <para>
    /// A tenant with no type ticked falls back to that legacy code, so data from before this column existed
    /// behaves as it did. See <c>WithdrawableDocumentTypeResolver</c>.
    /// </para>
    /// </summary>
    public virtual bool IsWithdrawable { get; set; }

    /// <summary>
    /// Whether this type is offered to employees when they raise a request in self-service (legacy
    /// <c>IncludeInWorkflow</c>). Defaults to true: a type from before this column existed was offered, and
    /// goes on being offered.
    /// </summary>
    public virtual bool IncludeInWorkflow { get; set; } = true;

    /// <summary>
    /// Whether documents of this type are kept out of the employee's own view in self-service (legacy
    /// <c>IsHiddenFromEmployee</c>) -- something HR records about an employee rather than for them.
    /// </summary>
    public virtual bool IsHiddenFromEmployee { get; set; }

    /// <summary>
    /// Whether a document of this type must carry an expiry date (legacy <c>IsExpiryDateRequired</c>). It also
    /// decides how two documents of the type are compared when <see cref="AllowDateOverlap"/> is off: by
    /// their whole periods when an expiry is required, by issue date alone when it is not.
    /// </summary>
    public virtual bool IsExpiryDateRequired { get; set; }

    /// <summary>
    /// Whether an employee may hold two documents of this type whose dates overlap (legacy
    /// <c>AllowDateOverlap</c>). Off, as legacy defaults it, a renewal entered before the old document has
    /// run out is refused. See <c>EmployeeDocumentDateRules</c>.
    /// </summary>
    public virtual bool AllowDateOverlap { get; set; }
}
