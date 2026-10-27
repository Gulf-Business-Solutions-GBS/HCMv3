using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Notification rule configuration (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.NotificationMaster).
///
/// SIMPLIFICATIONS (documented per porting convention, not silently dropped):
/// - Legacy NotificationType was a dynamic System.Type picker (any persistent XAF class,
///   resolved via XPObjectType reflection). Arbitrary reflection-based type binding has no clean ABP/EF
///   Core equivalent, so NotificationType here is a plain free-text string naming the
///   entity/scenario (e.g. "AbsenceRequest") instead of an actual Type reference.
/// - Legacy NotificationSubType's dynamic multi-source lookup logic (string-matching the type
///   name to resolve against AbsenceType/DocumentType/DisciplinaryType) is XAF-specific
///   UI-editor plumbing and is dropped entirely -- not ported in any form.
/// - Legacy Criterion was edited via XAF's dynamic criteria-editor UI component bound to
///   NotificationType. Here it is stored as plain freeform text with no criteria-editor semantics.
///
/// NotificationConfig reuses the existing HCMv3.GeneralMasterInfo.NotificationConfig
/// entity, already ported.
///
/// RULE ENGINE (2026-10-19, legacy gap batch 8 round B, section 3). The three simplifications above are now
/// replaced, not just documented:
/// - <see cref="NotificationType"/> holds a rule source code (<c>HcmNotifications.NotificationRuleSourceCodes</c>):
///   a whitelisted kind of record, not a free-text name and not an arbitrary type.
/// - <see cref="NotificationSubTypeId"/> is the sub type, and unlike legacy it actually filters.
/// - <see cref="Criterion"/> holds a business rule engine expression (the JSON the formula editor produces),
///   evaluated against the fields the source declares. Legacy XAF criteria text does not parse as one; such a
///   rule is skipped (and logged) until it is re-entered.
/// What a rule has already sent about is kept per rule in <c>HcmNotifications.NotificationSentLog</c>, not in
/// a flag on the record.
/// </summary>
public class NotificationMaster : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Plain entity/scenario name. SIMPLIFICATION: replaces the legacy dynamic System.Type picker --
    /// see class remarks.
    /// </summary>
    [Required]
    [StringLength(256)]
    public virtual string NotificationType { get; set; }

    /// <summary>
    /// The sub type the rule is limited to -- a document type, absence type, disciplinary type or loan type,
    /// depending on <see cref="NotificationType"/>. Not a foreign key: which table it points into depends on
    /// the type. Null means every sub type.
    /// </summary>
    public virtual int? NotificationSubTypeId { get; set; }

    public virtual int NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }

    /// <summary>
    /// Freeform criteria text. SIMPLIFICATION: no criteria-editor semantics -- see class remarks.
    /// </summary>
    public virtual string Criterion { get; set; }

    public virtual bool IsEnabled { get; set; }
}
