using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Third-party time-attendance device/API integration configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.TimeAttAPI</c>, Wave 4). Typically a single/few rows per
/// tenant.
///
/// <see cref="Password"/> is stored encrypted via <c>IntegrationSecret.Protect</c> (legacy stored it as plain
/// text). Rows saved before encryption was introduced may still hold plain text; every reader goes through
/// the tolerant <c>IntegrationSecret.Reveal</c>, and the value is re-encrypted on its next save.
///
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class TimeAttAPI : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(500)]
    public virtual string TokenUrl { get; set; }

    [StringLength(256)]
    public virtual string Username { get; set; }

    /// <summary>
    /// Ciphertext (see class remarks). Never send it to the client. The column allows 256 characters, so
    /// the plain-text password is capped lower on input to leave room for the encryption overhead.
    /// </summary>
    [StringLength(256)]
    public virtual string Password { get; set; }

    [StringLength(500)]
    public virtual string TransactionsUrl { get; set; }

    public virtual int NoOfRecords { get; set; }

    [StringLength(500)]
    public virtual string DevicesUrl { get; set; }

    [StringLength(50)]
    public virtual string PunchInCode { get; set; }

    [StringLength(50)]
    public virtual string PunchOutCode { get; set; }

    [StringLength(50)]
    public virtual string BreakOutCode { get; set; }

    [StringLength(50)]
    public virtual string BreakInCode { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(256)]
    public virtual string Company { get; set; }
}
