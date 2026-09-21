using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Integrations;

/// <summary>
/// Per-tenant configuration for one integration, one row per field.
///
/// This is the default home for every integration added from the Integration Center onwards. It is
/// deliberately NOT a universal destination for all existing integration config: SMTP and Azure SSO are
/// read straight out of ABP's <c>SettingManager</c> by the framework's own e-mail and login pipelines,
/// and webhook subscriptions are one-to-many rather than one config row, so those three keep their
/// framework storage and the Integration Center writes through to it. Dynamics 365 moved here because its
/// settings were already generic key/value rows -- and because <see cref="IsSecret"/> is the point.
///
/// Values with <see cref="IsSecret"/> set are encrypted at rest with <c>SimpleStringCipher</c> (the same
/// mechanism the SMTP password and tenant connection strings already use) and are never returned to the
/// browser: the configuration form shows a masked placeholder and accepts writes only.
/// </summary>
public class IntegrationSetting : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>One of the <see cref="IntegrationKeys"/> constants.</summary>
    [Required]
    [StringLength(MaxIntegrationKeyLength)]
    public virtual string IntegrationKey { get; set; }

    /// <summary>Field name within that integration, e.g. <c>CLIENT_SECRET</c>.</summary>
    [Required]
    [StringLength(MaxSettingKeyLength)]
    public virtual string SettingKey { get; set; }

    /// <summary>
    /// The stored value -- ciphertext when <see cref="IsSecret"/> is set, plain text otherwise.
    /// Sized well above the plaintext limit because encryption inflates the string.
    /// </summary>
    [StringLength(MaxValueLength)]
    public virtual string Value { get; set; }

    /// <summary>
    /// Whether <see cref="Value"/> is encrypted and withheld from the client. Stored per row rather than
    /// derived from the connector's field metadata so that a row can still be decrypted correctly if a
    /// field is later reclassified.
    /// </summary>
    public virtual bool IsSecret { get; set; }

    public const int MaxIntegrationKeyLength = 64;
    public const int MaxSettingKeyLength = 128;
    public const int MaxValueLength = 2000;
}
